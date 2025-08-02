using System.Reflection;

namespace WeddingAPI.Helpers;

public class EndpointMapper : IEndpointMapper
{
  public void MapEndpoints(WebApplication app)
  {
    var mappers = Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => typeof(IEndpointMapper).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
        .Select(t => Activator.CreateInstance(t) as IEndpointMapper)
        .Where(m => m != null);

    foreach (var mapper in mappers)
    {
      mapper!.MapEndpoints(app);
    }
  }
}