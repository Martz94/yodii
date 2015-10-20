using System.Diagnostics;
using Yodii.Model;

namespace Yodii.Engine.Tests.Mocks
{
    public class ItemInfo : IDiscoveredItem
    {
        readonly string _fullName;
        readonly IAssemblyInfo _assemblyInfo;
        string _errorMessage;

        internal ItemInfo(string fullName, IAssemblyInfo assemblyInfo, string errorMessage = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(fullName));
            Debug.Assert(assemblyInfo != null);

            _fullName = fullName;
            _assemblyInfo = assemblyInfo;
            _errorMessage = errorMessage;
        }
        public string FullName { get { return _fullName; } }
        public IAssemblyInfo AssemblyInfo { get { return _assemblyInfo; } }
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }
        public bool HasError
        {
            get { return _errorMessage != null; }
        }
    }
}
