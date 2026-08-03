namespace HoraLife.Store.Domain.Enums;

public enum ProductType
{
    Gemstone,
    Yantra,
    Rudraksha,
    PujaItem,
    RemedyKit,
    Other
}

public enum CartStatus
{
    Active,
    Converted,
    Abandoned,
    Expired
}

public enum AddressType
{
    Shipping,
    Billing
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Returned
}

public enum OrderPaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Failed,
    Refunded,
    PartiallyRefunded
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Captured,
    Failed,
    Refunded,
    PartiallyRefunded
}

public enum OrderSource
{
    DirectStore,
    KundaliRecommendation,
    CompatibilityRecommendation,
    Admin
}

public enum ShipmentStatus
{
    Pending,
    ReadyToShip,
    Shipped,
    InTransit,
    OutForDelivery,
    Delivered,
    Failed,
    Returned
}

public enum InventoryTransactionType
{
    StockAdded,
    OrderReserved,
    OrderConfirmed,
    ReservationReleased,
    ReturnReceived,
    ManualAdjustment
}
