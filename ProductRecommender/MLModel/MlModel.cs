using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using ProductRecommender.Controllers;
using ProductRecommender.Models;

namespace ProductRecommender.Model
{
    public class MlModel
    {
        private MLContext _mlContext;

        public MlModel(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void TestModel()
        {
            _mlContext = new MLContext();
            (IDataView trainingDataView, IDataView testDataView) = LoadData(_mlContext);
            ITransformer model = BuildAndTrainModel(_mlContext, trainingDataView);
            EvaluateModel(_mlContext, testDataView, model);
            UseModelForSinglePrediction(_mlContext, model);
            PredictAll(_mlContext, model);
            SaveModel(_mlContext, trainingDataView.Schema, model);
        }

        public void MyTestLoad()
        {
            _mlContext = new MLContext();
            var modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "ProductRecommenderModel.zip");
            DataViewSchema modelSchema;
            ITransformer trainedModel = _mlContext.Model.Load(modelPath, out modelSchema);
            PredictAll(_mlContext, trainedModel);
            (IDataView trainingDataView, IDataView testDataView) = LoadData(_mlContext);
            ITransformer model = BuildAndTrainModel(_mlContext, trainingDataView);
            PredictAll(_mlContext, trainedModel);
        }

        public void MyTestLoadNew()
        {
            _mlContext = new MLContext();
            (IDataView trainingDataView, IDataView testDataView) = LoadData(_mlContext);
            ITransformer model = BuildAndTrainModel(_mlContext, trainingDataView);
            EvaluateModel(_mlContext, testDataView, model);
            PredictAll(_mlContext, model);
            EvaluateModel(_mlContext, testDataView, model);
            SaveModel(_mlContext, trainingDataView.Schema, model);
        }

        public static void ReTrainModel()
        {
            MLContext _mlContext = new MLContext();
            (IDataView trainingDataView, IDataView testDataView) = LoadData(_mlContext);
            ITransformer model = BuildAndTrainModel(_mlContext, trainingDataView);
            // PredictAll(_mlContext, model);
            SaveModel(_mlContext, trainingDataView.Schema, model);
        }

        public static (IDataView training, IDataView test) LoadData(MLContext mlContext)
        {
            DatabaseLoader loader = mlContext.Data.CreateDatabaseLoader<ProductRatingMl>();
            string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            string sqlComand = "SELECT customer_ratings AS UserId, product_ratings as ProductId, rating as Rating, id FROM ratings";
            // Npgsql.NpgsqlFactory.Instance
            DatabaseSource dbSource = new DatabaseSource(
                // SQLiteFactory.Instance,
                Npgsql.NpgsqlFactory.Instance,
                connectionString, sqlComand);
            IDataView dataView = loader.Load(dbSource);
            DataOperationsCatalog.TrainTestData dataSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.1);
            var tmp = dataView.Preview(maxRows: 10);

            IDataView trainingDataView =
                dataSplit.TrainSet;
            IDataView testDataView =
                dataSplit.TestSet;

            return (dataView, testDataView);
        }

        public static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView trainingDataView)
        {
            IEstimator<ITransformer> estimator = mlContext.Transforms.Conversion
                .MapValueToKey(outputColumnName: "UserIdEncoded", inputColumnName: "UserId")
                .Append(mlContext.Transforms.Conversion.MapValueToKey(
                    outputColumnName: "ProductIdEncoded",
                    inputColumnName: "ProductId"));
            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "UserIdEncoded",
                MatrixRowIndexColumnName = "ProductIdEncoded",
                LabelColumnName = "Rating",
                NumberOfIterations = 20,
                Lambda = 0.05f,
                ApproximationRank = 64,
                Quiet = true,
                // Lambda = 0.001f
            };
            var trainerEstimator = estimator.Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));
            Debug.WriteLine("=============== Training the model ===============");
            var preview = trainerEstimator.Preview(trainingDataView, maxRows: 10);
            ITransformer model = trainerEstimator.Fit(trainingDataView);
            return model;
        }

        public static void EvaluateModel(MLContext mlContext, IDataView testDataView, ITransformer model)
        {
            Console.WriteLine("=============== Evaluating the model ===============");
            var prediction = model.Transform(testDataView);
            var metrics =
                mlContext.Regression.Evaluate(prediction, labelColumnName: "Rating", scoreColumnName: "Score");
            Console.WriteLine("Root Mean Squared Error : " + metrics.RootMeanSquaredError.ToString());
            Console.WriteLine("RSquared: " + metrics.RSquared.ToString());
        }

        public static void PredictAll(MLContext mlContext, ITransformer model)
        {
            Console.WriteLine("=============== Making a prediction ===============");
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ProductRatingMl, RatingPrediction>(model);

            var pedro = "34b357c6-4f3b-4eb6-af4e-e2a5367ad58e";
            var som = "74c5626f-d9fd-3e60-58f0-de76324067cc";
            List<ProductWithRatingPrediction> tmp = new List<ProductWithRatingPrediction>();
            for (int i = 0; i < 52; i++)
            {
                var testInput = new ProductRatingMl() {UserId = 1,ProductId = i};
                var courseRatingPrediction = predictionEngine.Predict(testInput);
                var label = Math.Round(courseRatingPrediction.Rating, 1);
                var score = Math.Round(courseRatingPrediction.Score, 1);
                tmp.Add(new ProductWithRatingPrediction
                {
                    ProductId = i,
                    RatingPrediction = new RatingPrediction
                    {
                        Rating = (float) label,
                        Score = (float) score
                    }
                });
            }

            tmp = tmp.OrderByDescending(c => c.RatingPrediction.Score).ToList();

            foreach (var courseWithRatingPrediction in tmp)
            {
                Console.WriteLine(
                    $"{courseWithRatingPrediction.ProductId} -- {courseWithRatingPrediction.RatingPrediction.Rating} --- {courseWithRatingPrediction.RatingPrediction.Score}");
            }

            Console.WriteLine($"Done");
        }

        public static void UseModelForSinglePrediction(MLContext mlContext, ITransformer model)
        {
            Console.WriteLine("=============== Making a prediction ===============");
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ProductRatingMl, RatingPrediction>(model);
            var testInput = new ProductRatingMl() {UserId = 1, ProductId = 10};

            var courseRatingPrediction = predictionEngine.Predict(testInput);
            if (Math.Round(courseRatingPrediction.Score, 1) > 3.5)
            {
                Console.WriteLine("Course " + testInput.ProductId + " is recommended for user " + testInput.UserId);
            }
            else
            {
                Console.WriteLine("Course " + testInput.ProductId + " is not recommended for user " + testInput.UserId);
            }
        }

        public static void SaveModel(MLContext mlContext, DataViewSchema trainingDataViewSchema, ITransformer model)
        {
            var modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "ProductRecommenderModel.zip");

            Console.WriteLine("=============== Saving the model to a file ===============");
            mlContext.Model.Save(model, trainingDataViewSchema, modelPath);
            Console.WriteLine("=============== Saved =================================");
        }

        public static async Task SaveModelAsync(MLContext mlContext, DataViewSchema trainingDataViewSchema,
            ITransformer model)
        {
            var modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "ProductRecommenderModel.zip");

            Console.WriteLine("=============== Saving the model to a file ===============");
            mlContext.Model.Save(model, trainingDataViewSchema, modelPath);
            Console.WriteLine("=============== Saved =================================");
            Console.WriteLine("Updating date");
            await Task.Delay(50);
            // DateTime fileTime = DateTime.Now.AddSeconds(1); 
            // File.SetLastWriteTime(modelPath, fileTime); 
            // File.SetLastAccessTime(modelPath, fileTime); 
            Console.WriteLine("Updated date");
            await Task.Delay(50);
        }
    }
}