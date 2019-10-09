namespace Megumin.DCS
{
    /// <summary>
    /// 
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// 
        /// </summary>
        int GUID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="deltaTime"></param>
        void Update(double deltaTime);
        /// <summary>
        /// 
        /// </summary>
        void Start();
    }
}