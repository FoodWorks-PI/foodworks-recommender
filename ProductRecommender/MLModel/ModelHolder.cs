using Microsoft.ML;

namespace ProductRecommender.Model
{
    public class ModelHolder
    {
        public ITransformer Model { get; set; }
    }
}