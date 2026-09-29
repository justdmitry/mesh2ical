namespace SchoolHelper.Mesh
{
    public class MealsV3OrdersResponseItem
    {
        public bool hasNext { get; set; }
        public Order[] orders { get; set; } = [];

        public class Order
        {
            public long orderId { get; set; }
            public DateTime createdAt { get; set; }
            public int status { get; set; }
            public int orderType { get; set; }
            public int deliveryWay { get; set; }
            public int provisionTerm { get; set; }
            public int[] listTypes { get; set; } = [];
            public DateTime? expiredAt { get; set; }
            public DateTime? deliveredAt { get; set; }
            public int price { get; set; }
            public int discount { get; set; }
            public int totalPrice { get; set; }
            public string onDate { get; set; } = string.Empty;
            //public Item[] items { get; set; }
        }

        //public class Item
        //{
        //    public Dish dish { get; set; }
        //    public Complex complex { get; set; }
        //}

        //public class Dish
        //{
        //    public int id { get; set; }
        //    public string name { get; set; }
        //    public int price { get; set; }
        //    public int categoryId { get; set; }
        //    public string categoryName { get; set; }
        //    public int amount { get; set; }
        //    public object regularRule { get; set; }
        //}

        //public class Complex
        //{
        //    public int id { get; set; }
        //    public string name { get; set; }
        //    public int price { get; set; }
        //    public int kind { get; set; }
        //    public int[] paymentTypes { get; set; }
        //    public bool preorderAllowed { get; set; }
        //    public bool allowSelectItems { get; set; }
        //    public bool specialDiet { get; set; }
        //    public int amount { get; set; }
        //    public ComplexItem[] complexItems { get; set; }
        //    public object regularRule { get; set; }
        //}

        //public class ComplexItem
        //{
        //    public int id { get; set; }
        //    public string name { get; set; }
        //    public int price { get; set; }
        //    public int categoryId { get; set; }
        //    public string categoryName { get; set; }
        //    public int amount { get; set; }
        //    public object regularRule { get; set; }
        //}
    }
}