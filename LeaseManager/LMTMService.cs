
namespace DADTKV.leaseManager
{
    class LMTMService : LMTMCommunicationService.LMTMCommunicationServiceBase
    {
        private LeaseManager _leaseManager;

        public LMTMService(LeaseManager leaseManager)
        {
            _leaseManager = leaseManager;
        }
    }
}

