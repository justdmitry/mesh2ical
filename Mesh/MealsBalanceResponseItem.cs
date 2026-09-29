namespace SchoolHelper.Mesh
{
    public class MealsBalanceResponseItem
    {
        public ClientId clientId { get; set; }
        public long contractId { get; set; }
        public long balance { get; set; }
        public ExpenseConstraints expenseConstraints { get; set; }
    }

    public class ClientId
    {
        public string personId { get; set; }
        public object staffId { get; set; }
    }

    public class ExpenseConstraints
    {
        public object expenseDayLimit { get; set; }
        public int balanceThreshold { get; set; }
    }
}
