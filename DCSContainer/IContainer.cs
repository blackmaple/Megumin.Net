using System.Threading.Tasks;
using Megumin.Remote;
using Net.Remote;

namespace Megumin.DCS
{
    /// <summary>
    /// 
    /// </summary>
    public interface IContainer
    {
        /// <summary>
        /// 
        /// </summary>
        IRemote Remote { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="rpcResult"></typeparam>
        /// <param name="testMessage"></param>
        /// <returns></returns>
        Task<rpcResult> Send<rpcResult>(object testMessage);
    }
}