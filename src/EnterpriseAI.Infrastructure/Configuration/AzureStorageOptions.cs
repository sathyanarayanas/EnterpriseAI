using System;
using System.Collections.Generic;
using System.Text;

namespace EnterpriseAI.Infrastructure.Configuration
{
    public class AzureStorageOptions
    {
        public string ConnectionString { get; set; } = string.Empty;

        public string ContainerName { get; set; } =
            "enterprise-documents";
    }
}
