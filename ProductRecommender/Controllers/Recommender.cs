using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Microsoft.ML;
using ProductRecommender.Database;
using ProductRecommender.Model;
using ProductRecommender.Models;

namespace ProductRecommender.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Recommender : ControllerBase
    {
        // private readonly PredictionEnginePool<CourseRatingMl, RatingPrediction> _predictionEnginePool;
        private ProductDbContext _dbContext;
        private MLContext _mlContext;
        private ModelHolder _modelHolder;

        double maxRating;
        int maxAttempts;
        int requiredCourses;
        int productsPerAttempt;

        public Recommender(
            // PredictionEnginePool<CourseRatingMl, RatingPrediction> predictionEnginePool,
            ProductDbContext dbContext, MLContext mlContext, ModelHolder modelHolder)
        {
            // _predictionEnginePool = predictionEnginePool;
            _dbContext = dbContext;
            _mlContext = mlContext;
            _modelHolder = modelHolder;
            maxRating = 5.0;
            maxAttempts = 3;
            productsPerAttempt = 52;
            requiredCourses = 5;
        }


        [HttpPost("retrain")]
        public OkResult ForceRetrain()
        {
            SaveModel();
            return Ok();
        }

        public void SaveModel()
        {
            (IDataView trainingDataView, IDataView testDataView) = MlModel.LoadData(_mlContext);
            ITransformer model = MlModel.BuildAndTrainModel(_mlContext, trainingDataView);
            _modelHolder.Model = model;
            MlModel.SaveModel(_mlContext, trainingDataView.Schema, model);
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<string>> Get(int userId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            List<Product> randProducts;

            List<ProductWithRatingPrediction> finalReccomendations = new List<ProductWithRatingPrediction>();
            for (int i = 0; i < maxAttempts; i++)
            {
                continue;
                // Get randombly unwathced courses

                var tmpProducts = await _dbContext.Products
                        .Include(c => c.ProductRatings)
                        .Where(c => c.ProductRatings.All(r => r.UserId != userId))
                        .ToListAsync()
                    ;

                randProducts = tmpProducts
                    .OrderBy(c => Guid.NewGuid())
                    .Take(productsPerAttempt).ToList();

                // Run prediction on those
                var engine =
                    _mlContext.Model.CreatePredictionEngine<ProductRatingMl, RatingPrediction>(_modelHolder.Model);
                
                var recommendations = randProducts
                    .Select(c => new ProductWithRatingPrediction()
                    {
                        ProductId = c.Id,
                        RatingPrediction = engine.Predict(
                            example: new ProductRatingMl
                            {
                                UserId = userId,
                                ProductId = c.Id
                            })
                    })
                    .Where(c => c.RatingPrediction.Score > maxRating * .5)
                    .ToList();
                
                engine.Dispose();

                finalReccomendations.AddRange(recommendations);

                if (finalReccomendations.Count >= requiredCourses)
                {
                    return Ok(finalReccomendations
                        .OrderByDescending(c => c.RatingPrediction.Score)
                        .Select(c => c.ProductId)
                        .Take(requiredCourses)
                        .ToList());
                }
            }

            // Else return empty
            return Ok(new List<Product>());
        }
    }

    class ProductWithRatingPrediction
    {
        public int ProductId { get; set; }
        public RatingPrediction RatingPrediction { get; set; }
    }
}