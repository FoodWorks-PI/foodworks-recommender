using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using ProductRecommender.Database;

namespace ProductRecommender.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class User : ControllerBase
    {
        private ProductDbContext _dbContext;

        private string uploads;

        public User(ProductDbContext dbContext)
        {
            _dbContext = dbContext;
            uploads = Path.Combine(Environment.CurrentDirectory, "Uploads");
        }

        // GET
        [HttpPost("{userId}/photo")]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
        public async Task<IActionResult> UploadPhoto(String userId, IFormFile image)
        {
            if (image.Length > 0)
            {
                Directory.CreateDirectory(uploads);
                var files = Directory.GetFiles(uploads, $"{userId}.*");
                if (files.Length > 0)
                {
                    System.IO.File.Delete(files[0]);
                }

                String guuid = userId;
                String name = $"{guuid}{Path.GetExtension(image.FileName)}";
                using (var fileStream = new FileStream(Path.Combine(uploads, name), FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                    return Ok();
                }
            }

            return BadRequest();
        }

        [HttpGet("{userId}/photo")]
        [Route("Stream")]
        public IActionResult DownloadImage(string userId)
        {
            // Since this is just and example, I am using a local file located inside wwwroot
            // Afterwards file is converted into a stream
            Directory.CreateDirectory(uploads);
            var files = Directory.GetFiles(uploads, $"{userId}.*");
            if (files.Length == 0)
            {
                return NotFound();
            }

            var filePath = files[0];
            var filestream = new FileStream(filePath, FileMode.Open);
            String contentType;
            new FileExtensionContentTypeProvider().TryGetContentType(filePath, out contentType);
            String name = $"ProfileImage{Path.GetExtension(filePath)}";
            return File(filestream, contentType, name);
        }
    }
}