using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frontend.Models.Dashboard
{
    public class DashboardStats
    {
        public int ActiveProjects { get; set; }

        public int PendingTasks { get; set; }

        public int ActiveClients { get; set; }

        public decimal Revenue { get; set; }
    }

    public class ActivityItem
    {
        public string Title { get; set; } = "";

        public string Description { get; set; } = "";
    }

    public class QuickAction
    {
        public string Name { get; set; } = "";
    }
}