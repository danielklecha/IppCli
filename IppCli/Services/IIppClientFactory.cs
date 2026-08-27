using IppCli.Models;
using SharpIpp;

namespace IppCli.Services;

public interface IIppClientFactory
{
    ISharpIppClient CreateClient(IIppSettings settings);
}
