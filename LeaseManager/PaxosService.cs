using Grpc.Core;

namespace DADTKV.leaseManager
{
    class PaxosService : PaxosCommunicationService.PaxosCommunicationServiceBase
    {
        private LeaseManager _lm;

        public PaxosService(LeaseManager leaseManager)
        {
            _lm = leaseManager;
        }

        public override Task<Promise> SendPrepare(PrepareRequest request, ServerCallContext context)
        {
            return Task.FromResult(PrepareImpl(request));
        }

        public Promise PrepareImpl(PrepareRequest request)
        {
            var resp = new Promise();

            if (!_lm.LmPaxos.IsDown() && !_lm.LmPaxos.IsLeader() && _lm.LmPaxos.LmPaxosTuple.ReadTimestamp < request.ReadTimestamp)
            {
                resp.ReadTimestamp = request.ReadTimestamp;
                _lm.LmPaxos.LmPaxosTuple.ReadTimestamp = request.ReadTimestamp;
                if (_lm.LmPaxos.LmPaxosTuple.Value != null)
                {
                    resp.Val = _lm.LmPaxos.LmPaxosTuple.Value;
                }
                else
                {
                    resp.Val = null;
                }
            }
            else
            {
                resp.Val = null;
                resp.ReadTimestamp = -1;
            }

            return resp;
        }

        public override Task<Accepted> SendAccept(AcceptRequest request, ServerCallContext context)
        {
            return Task.FromResult(AcceptImpl(request));
        }

        public Accepted AcceptImpl(AcceptRequest request)
        {
            var resp = new Accepted();
            resp.Accepted_ = false;
            if (!_lm.LmPaxos.IsDown() && !_lm.LmPaxos.IsLeader() && request.WriteTimestamp == _lm.LmPaxos.LmPaxosTuple.ReadTimestamp)
            {
                _lm.LmPaxos.LmPaxosTuple.WriteTimestamp = request.WriteTimestamp;
                _lm.LmPaxos.LmPaxosTuple.Value = request.Val;
                resp.Accepted_ = true;
            }

            return resp;
        }

        public override Task<SendLeaseListResponse> SendLeaseList(SendLeaseListRequest request, ServerCallContext context)
        {
            return Task.FromResult(SendLeaseListImpl(request));
        }

        public SendLeaseListResponse SendLeaseListImpl(SendLeaseListRequest request)
        {
            var resp = new SendLeaseListResponse();
            var leaseList = new ReceiveLeaseListRequest();
            leaseList.LeaseList = request.LeaseList;
            leaseList.RequestId = request.RequestId;
            foreach(var tm in _lm.TmsClients)
            {
                tm.ReceiveLeaseListAsync(leaseList);
            }
            return resp;
        }
    }
}

