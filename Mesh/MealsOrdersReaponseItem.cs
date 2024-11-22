namespace SchoolHelper.Mesh
{
    public class MealsOrdersReaponseItem
    {
        public long id { get; set; }
        //public DateTimeOffset createdAt { get; set; }
        //public DateTimeOffset? deliveredAt { get; set; }
        //public DateTimeOffset? expiredAt { get; set; }
        //public int deliveryWay { get; set; }
        //public string name { get; set; } = string.Empty;
        //public int status { get; set; }
        //public int provisionTermsTypeId { get; set; }
        //public int price { get; set; }
        //public int? orderType { get; set; }
        //public DateOnly? onDate { get; set; }
        public MealItem[] items { get; set; } = [];
    }

    public class MealItem
    {
        public Dish? dish { get; set; }
        public Complex? complex { get; set; }
    }

    public class Dish
    {
        public int dishId { get; set; }
        public string name { get; set; } = string.Empty;
        //public int price { get; set; }
        //public int buffetCategoryId { get; set; }
        //public string buffetCategoryName { get; set; } = string.Empty;
        //public int amount { get; set; }
    }

    public class Complex
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        //public DateOnly? startDate { get; set; }
        //public DateOnly? endDate { get; set; }
        //public int price { get; set; }
        //public int amount { get; set; }
        //public int kind { get; set; }
        //public int paymentType { get; set; }
        //public bool preorderAllowed { get; set; }
        //public bool allowSelectItems { get; set; }
        public ComplexItem[] items { get; set; } = [];
    }

    public class ComplexItem
    {
        public int dishId { get; set; }
        public string name { get; set; } = string.Empty;
        //public int price { get; set; }
        //public object buffetCategoryId { get; set; } = string.Empty;
        //public object buffetCategoryName { get; set; } = string.Empty;
        //public int amount { get; set; }
    }
}
