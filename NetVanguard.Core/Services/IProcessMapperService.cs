using NetVanguard.Core.Models;

namespace NetVanguard.Core.Services
{
    public interface IProcessMapperService
    {
        NetworkApplication GetOrResolveApplication(int processId);
    }
}
