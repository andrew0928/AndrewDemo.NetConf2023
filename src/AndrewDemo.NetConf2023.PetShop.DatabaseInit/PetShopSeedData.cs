namespace AndrewDemo.NetConf2023.PetShop.DatabaseInit
{
    internal static class PetShopSeedData
    {
        public static IReadOnlyList<ProductSeed> Products => new[]
        {
            new ProductSeed(
                "pet-food-premium",
                "SKU-PET-FOOD-PREMIUM",
                "高蛋白成犬鮮食包",
                "適合成犬日常補充蛋白質的主食鮮食包。",
                1500m,
                100),
            new ProductSeed(
                "pet-carrier-bag",
                "SKU-PET-CARRIER-BAG",
                "透氣寵物外出包",
                "小型犬貓適用的透氣外出包。",
                1200m,
                30),
            new ProductSeed(
                "pet-shampoo-sensitive",
                "SKU-PET-SHAMPOO-SENSITIVE",
                "敏感肌寵物洗毛精",
                "溫和配方，適合敏感肌犬貓使用。",
                450m,
                100),
            new ProductSeed(
                "pet-toy-rope",
                "SKU-PET-TOY-ROPE",
                "棉繩潔牙玩具",
                "協助寵物玩耍與日常潔牙的棉繩玩具。",
                300m,
                100)
        };
    }

    internal sealed record ProductSeed(
        string ProductId,
        string SkuId,
        string Name,
        string Description,
        decimal Price,
        int InventoryQuantity);
}
