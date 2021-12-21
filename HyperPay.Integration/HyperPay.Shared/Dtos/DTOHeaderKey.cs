using System.Collections.Generic;

namespace HyperPay.Shared.Dtos
{
    public class DTOHeaderKey
    {
        public string Key { get; set; }
        public HashSet<string> Value { get; set; }
    }
}
