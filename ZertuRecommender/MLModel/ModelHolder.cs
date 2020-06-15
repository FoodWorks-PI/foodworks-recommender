using Microsoft.ML;

namespace ZertuRecommender.Model
{
    public class ModelHolder
    {
        public ITransformer Model { get; set; }
    }
}