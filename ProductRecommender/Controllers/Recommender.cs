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
        int coursesPerAttempt;

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
            coursesPerAttempt = 52;
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


        [HttpPost("preferences/{userId}")]
        public async Task<ActionResult> SetPreferences(String userId, [FromBody] UserPreferences userPreferences)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var procecedPreferences = userPreferences.Categories.Select(c => new CourseRating
            {
                UserId = userId,
                CourseId = -(c + 2),
                Rating = (float) (maxRating * 0.75)
            }).ToList();

            foreach (var rating in procecedPreferences)
            {
                _dbContext.CourseRatings.Add(rating);
            }

            await _dbContext.SaveChangesAsync();

            SaveModel();
            return Ok();
        }

        [HttpPut]
        public async Task<ActionResult<string>> UpdateRatings([FromBody] UserView updateData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var viewed =
                await _dbContext.UserViews
                    .Where(c =>
                        c.CourseId == updateData.CourseId && c.UserId == updateData.UserId)
                    .ToListAsync();

            bool contained = viewed.Any(c => c.VideoId == updateData.VideoId);
            if (contained)
            {
                return Ok();
            }

            // get course
            var course = await _dbContext.Courses.FirstOrDefaultAsync(c => c.CourseId == updateData.CourseId);

            // Get Course Rating
            var courseRating =
                await _dbContext.CourseRatings
                    .FirstOrDefaultAsync(c =>
                        c.CourseId == updateData.CourseId && c.UserId == updateData.UserId);

            // Increment Views 
            var viewedCount = viewed.Count + 1;
            _dbContext.UserViews.Add(new UserView
            {
                UserId = updateData.UserId,
                CourseId = updateData.CourseId,
                VideoId = updateData.VideoId
            });
            // Increment Rating 
            var rating = (float) ((float) viewedCount / course.VideoCount * maxRating);
            if (courseRating == null)
            {
                courseRating = new CourseRating
                {
                    UserId = updateData.UserId,
                    CourseId = updateData.CourseId,
                    Rating = rating
                };
            }
            else
            {
                courseRating.Rating = rating;
            }

            _dbContext.CourseRatings.Update(courseRating);
            await _dbContext.SaveChangesAsync();
            SaveModel();
            return Ok();
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<string>> Get(String userId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            List<Course> randCourses;

            List<CourseWithRatingPrediction> finalReccomendations = new List<CourseWithRatingPrediction>();
            for (int i = 0; i < maxAttempts; i++)
            {
                // Get randombly unwathced courses

                var tmpCourses = await _dbContext.Courses
                        .Include(c => c.CourseRatings)
                        .Where(c => c.CourseRatings.All(r => r.UserId != userId) && c.CourseId >= 0)
                        .ToListAsync()
                    ;

                randCourses = tmpCourses
                    .OrderBy(c => Guid.NewGuid())
                    .Take(coursesPerAttempt).ToList();

                // Run prediction on those
                var engine =
                    _mlContext.Model.CreatePredictionEngine<CourseRatingMl, RatingPrediction>(_modelHolder.Model);
                
                var recommendations = randCourses
                    .Select(c => new CourseWithRatingPrediction()
                    {
                        CourseId = c.CourseId,
                        RatingPrediction = engine.Predict(
                            example: new CourseRatingMl
                            {
                                UserId = userId,
                                CourseId = c.CourseId
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
                        .Select(c => c.CourseId)
                        .Take(requiredCourses)
                        .ToList());
                }
            }

            // Else return random from user preferences
            // Get preferences
            var categories = await _dbContext.CourseRatings
                .Where(c => c.UserId == userId && c.CourseId <= 0)
                // Hack: Category is - course for ghost courses
                .Select(c => -c.CourseId).ToListAsync();

            var randFromCat = await _dbContext.Courses
                // Get courses with category prefered
                .Where(c => categories.Any(cat => c.CategoryId == cat) && c.CourseId >= 0)
                .Include(c => c.CourseRatings)
                // Filter watched coursed
                .Where(c => c.CourseRatings.All(r => r.UserId != userId))
                .Select(c => c.CourseId)
                .ToListAsync();
            randFromCat = randFromCat.OrderBy(c => Guid.NewGuid()).Take(requiredCourses).ToList();

            return Ok(randFromCat);
        }
    }

    class CourseWithRatingPrediction
    {
        public int CourseId { get; set; }
        public RatingPrediction RatingPrediction { get; set; }
    }
}