namespace DADTKV.transactionManager
{
    /// <summary>
    /// Represents the order of the Leases.
    /// </summary>
    public struct LeaseSheet
    {
        public string tmID;

        public List<string> leases;

        public int order;

        public string GetTmID()
        {
            return tmID;
        }

        public List<string> GetLeases()
        {
            return leases;
        }

        public int GetOrder()
        {
            return order;
        }
    }
}
