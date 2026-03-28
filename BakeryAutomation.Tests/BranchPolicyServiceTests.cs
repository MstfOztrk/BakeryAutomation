using System;
using BakeryAutomation.Models;
using BakeryAutomation.Services;

namespace BakeryAutomation.Tests
{
    public sealed class BranchPolicyServiceTests
    {
        [Fact]
        public void GetNextCollectionDate_ClampsDayToLastDayOfShortMonth()
        {
            var branch = new Branch { PaymentDayOfMonth = 31 };
            var service = new BranchPolicyService();

            var nextCollectionDate = service.GetNextCollectionDate(branch, new DateTime(2026, 2, 1));

            Assert.Equal(new DateTime(2026, 2, 28), nextCollectionDate);
        }

        [Fact]
        public void GetNextCollectionDate_RollsToNextMonth_WhenCandidateIsPast()
        {
            var branch = new Branch { PaymentDayOfMonth = 10 };
            var service = new BranchPolicyService();

            var nextCollectionDate = service.GetNextCollectionDate(branch, new DateTime(2026, 3, 11));

            Assert.Equal(new DateTime(2026, 4, 10), nextCollectionDate);
        }
    }
}
