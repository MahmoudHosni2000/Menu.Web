# Session 07 — Orders Module

هذا الملف مرجع عملي لما تم تنفيذه في Session 07 داخل مشروع `Menu.Web`. الهدف أن تفهم رحلة الطلب كاملة، وتستطيع مراجعتها أو إعادة تنفيذها لاحقًا دون مساعدة خارجية.

## 1. ماذا بنينا؟

بنينا Orders API تستطيع:

- إنشاء طلب من سلة محفوظة في Redis.
- أخذ أسعار المنتجات الحقيقية من SQL بدل الثقة في بيانات السلة.
- اختيار وسيلة توصيل وحساب إجمالي الطلب.
- حفظ الطلب وعناصره وعنوان الشحن في SQL Server.
- عرض وسائل التوصيل.
- عرض طلبات المستخدم الحالي فقط.
- عرض طلب واحد بشرط أن يكون مملوكًا للمستخدم الحالي.
- حماية جميع عمليات الطلبات باستخدام JWT وRole باسم `User`.

المسار الكامل لإنشاء الطلب:

```text
Swagger / Client
→ OrdersController
→ OrderService
→ BasketRepository → Redis
→ GenericRepository → SQL Products + DeliveryMethod
→ UnitOfWork → Save Order + OrderItems
→ AutoMapper
→ OrderResponse
```

## 2. الفكرة بتشبيه المطعم

تخيل أن العميل يملك سلة فيها الوجبات التي اختارها، وعندما يؤكد الشراء يبدأ المطعم في إصدار فاتورة رسمية:

| الجزء التقني | التشبيه |
|---|---|
| `CustomerBasket` | ورقة اختيارات العميل المؤقتة |
| Redis | مكان سريع لحفظ عربات العملاء |
| `Product` من SQL | قائمة الأسعار الرسمية للمطعم |
| `DeliveryMethod` | شركة أو طريقة التوصيل |
| `Order` | الفاتورة الرسمية |
| `OrderItem` | سطر داخل الفاتورة |
| `ProductItemOrdered` | نسخة ثابتة من اسم وصورة المنتج وقت الشراء |
| `OrderAddress` | عنوان تسليم الفاتورة |
| `OrderService` | الموظف الذي يجهز الفاتورة ويحفظها |
| `OrdersController` | موظف الاستقبال الذي يتعامل مع العميل |

السلة مؤقتة، لكن الطلب سجل رسمي يجب أن يحفظ البيانات والسعر وقت الشراء.

## 3. تقسيم Session 07

قسمنا الجلسة إلى ستة أجزاء:

1. Order Domain Models.
2. EF Core Configurations وMigration.
3. Delivery Methods Seeding.
4. Order Service وإنشاء الطلب.
5. AutoMapper وCreate Order endpoint.
6. Query endpoints والاختبارات النهائية.

## 4. الجزء الأول — Domain Models

### OrderStatus

يمثل حالة الطلب، مثل:

```csharp
public enum OrderStatus
{
    Pending,
    PaymentReceived,
    PaymentFailed
}
```

الفائدة: نستخدم قيمًا محددة بدل كتابة نصوص مختلفة وعشوائية لحالة الطلب.

### OrderAddress

يحفظ عنوان الشحن داخل الطلب:

```text
FirstName
LastName
Street
City
Country
```

العنوان جزء من الطلب نفسه، وليس جدول عناوين مستقلًا.

### DeliveryMethod

يمثل وسيلة التوصيل، ويحتوي على:

```text
ShortName
Description
DeliveryTime
Price
```

مثل `UPS1` بسعر توصيل `10`.

### ProductItemOrdered

يحفظ لقطة ثابتة من بيانات المنتج وقت الشراء:

```csharp
public int ProductId { get; set; }
public string ProductName { get; set; }
public string PictureUrl { get; set; }
```

لماذا لا نخزن `ProductId` فقط؟ لأن اسم المنتج أو صورته قد يتغيران مستقبلًا، بينما الطلب القديم يجب أن يحتفظ بما اشتراه العميل وقتها.

### OrderItem

يمثل عنصرًا واحدًا داخل الطلب:

```text
Product  = لقطة بيانات المنتج
Price    = السعر الحقيقي وقت الشراء
Quantity = الكمية
```

### Order

هو الكيان الرئيسي للطلب، ويحتوي على:

```text
BuyerEmail
OrderDate
Status
ShippingAddress
DeliveryMethodId
DeliveryMethod
Items
SubTotal
```

ويحسب الإجمالي:

```csharp
public decimal GetTotal()
{
    return SubTotal + DeliveryMethod.Price;
}
```

```text
SubTotal = مجموع أسعار المنتجات × الكميات
Total    = SubTotal + سعر التوصيل
```

## 5. الجزء الثاني — EF Core Configurations

أنشأنا ثلاث Configurations:

```text
DeliveryMethodConfig
OrderItemConfig
OrderConfig
```

### OwnsOne مع ProductItemOrdered

```csharp
builder.OwnsOne(item => item.Product);
```

معناها أن `ProductItemOrdered` جزء مملوك لـ`OrderItem` وليس Entity مستقلة لها جدول منفصل.

التشبيه: بيانات المنتج سطر داخل الفاتورة، وليست فاتورة مستقلة.

### OwnsOne مع ShippingAddress

```csharp
builder.OwnsOne(order => order.ShippingAddress, address =>
{
    // Property configuration
});
```

عنوان الشحن يُحفظ داخل صف الطلب نفسه في أعمدة مخصصة.

### علاقة DeliveryMethod

```csharp
builder.HasOne(order => order.DeliveryMethod)
    .WithMany()
    .HasForeignKey(order => order.DeliveryMethodId)
    .OnDelete(DeleteBehavior.Restrict);
```

استخدمنا `Restrict` حتى لا يؤدي حذف وسيلة توصيل مستخدمة إلى كسر الطلبات القديمة.

### علاقة Order Items

```csharp
builder.HasMany(order => order.Items)
    .WithOne()
    .IsRequired()
    .OnDelete(DeleteBehavior.Cascade);
```

كل `OrderItem` يجب أن ينتمي إلى طلب، وإذا حذف الطلب تحذف عناصره معه.

بعد إضافة `DbSet` أنشأنا وطبقنا Migration باسم:

```text
AddOrderEntities
```

## 6. الجزء الثالث — Delivery Methods Seeding

أضفنا وسائل التوصيل من ملف `delivery.json` عند تشغيل التطبيق، ولكن فقط إذا كان الجدول فارغًا:

```text
UPS1
UPS2
UPS3
FREE
```

الخطوات داخل `DbInitializer`:

```text
فحص جدول DeliveryMethods
→ قراءة delivery.json
→ Deserialize إلى List<DeliveryMethod>
→ AddRangeAsync
→ SaveChangesAsync
```

لم نضع `Id` داخل JSON لأن العمود Identity وSQL Server هو المسؤول عن توليد قيمته.

## 7. الجزء الرابع — إنشاء الطلب داخل OrderService

### Dependencies

```csharp
IUnitOfWork
IMapper
IBasketRepository
```

| Dependency | وظيفتها |
|---|---|
| `IBasketRepository` | قراءة السلة من Redis |
| `IUnitOfWork` | قراءة المنتجات ووسيلة التوصيل وحفظ الطلب في SQL |
| `IMapper` | التحويل بين Entities وDTOs |

### خطوات CreateOrderAsync

#### 1. قراءة السلة

```csharp
var basket = await _basketRepository
    .GetBasketAsync(request.BasketId);

if (basket is null)
    return null;
```

#### 2. قراءة وسيلة التوصيل

استخدمنا Specification للتأكد أن `DeliveryMethodId` موجود فعلًا في SQL.

#### 3. تحويل عنوان الشحن

```csharp
var shippingAddress =
    _mapper.Map<OrderAddress>(request.ShipToAddress);
```

#### 4. تجهيز Order Items

مررنا على عناصر السلة، ثم جلبنا كل منتج من SQL باستخدام `ProductId`.

مهم جدًا: استخدمنا:

```csharp
Price = product.Price
```

ولم نستخدم سعر السلة؛ لأن العميل أو بيانات Redis قد تحتوي سعرًا قديمًا أو معدلًا.

```text
Basket تعطي ProductId والكمية
SQL يعطي الاسم والصورة والسعر الرسمي
```

#### 5. حساب SubTotal

```csharp
var subTotal = orderItems.Sum(
    item => item.Price * item.Quantity);
```

#### 6. إنشاء وحفظ Order

```csharp
var order = new Order
{
    BuyerEmail = userEmail,
    ShippingAddress = shippingAddress,
    DeliveryMethodId = deliveryMethod.Id,
    Items = orderItems,
    SubTotal = subTotal
};
```

ثم:

```csharp
await _unitOfWork
    .GetRepo<int, Order>()
    .AddAsync(order);

var savedChanges = await _unitOfWork.SaveChangesAsync();
```

وضعنا `DeliveryMethodId` قبل الحفظ بدل تمرير كائن `DeliveryMethod` غير المتتبع، حتى لا يظن EF Core أنها وسيلة جديدة ويحاول إضافتها مرة أخرى.

## 8. الجزء الخامس — AutoMapper

أنشأنا `OrderProfile` وفيه أربع خرائط رئيسية.

### Address Mapping

```csharp
CreateMap<OrderAddressDto, OrderAddress>()
    .ReverseMap();
```

يسمح بالتحويل في الاتجاهين.

### DeliveryMethod Mapping

```csharp
CreateMap<DeliveryMethod, DeliveryMethodResponse>();
```

الخصائص متطابقة في الاسم، لذلك لا نحتاج `ForMember`.

### OrderItem Mapping

بيانات المنتج متداخلة داخل Entity:

```text
OrderItem.Product.ProductId
```

لكنها مسطحة داخل DTO:

```text
OrderItemDto.ProductId
```

لذلك استخدمنا `ForMember`:

```csharp
.ForMember(
    destination => destination.ProductId,
    options => options.MapFrom(
        source => source.Product.ProductId))
```

اقرأها: عند ملء `ProductId` في الوجهة، خذ القيمة من `Product.ProductId` في المصدر.

### Order Mapping

الخصائص المختلفة في الاسم أو الشكل:

```text
BuyerEmail               → UserEmail
DeliveryMethod.ShortName → DeliveryMethod
GetTotal()               → Total
```

وسجلنا `OrderProfile` داخل `Program.cs`:

```csharp
config.AddProfile<OrderProfile>();
```

إنشاء Profile دون تسجيله يسمح للـBuild بالنجاح، لكنه يؤدي إلى Mapping Error وقت التشغيل.

## 9. ربط OrderService

سجلنا الخدمة في Dependency Injection:

```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
```

ثم أضفناها إلى `IServiceManager` و`ServiceManager`:

```csharp
IOrderService OrderService { get; }
```

التدفق:

```text
Program.cs ينشئ OrderService
→ ServiceManager يجمعها مع باقي الخدمات
→ OrdersController يحصل عليها
```

## 10. الجزء السادس — OrderSpecifications

أنشأنا constructor لجميع طلبات المستخدم:

```csharp
public OrderSpecifications(string userEmail)
    : base(order => order.BuyerEmail == userEmail)
```

وconstructor لطلب واحد:

```csharp
public OrderSpecifications(int orderId, string userEmail)
    : base(order =>
        order.Id == orderId &&
        order.BuyerEmail == userEmail)
```

أضفنا:

```csharp
AddInclude(order => order.DeliveryMethod);
AddInclude(order => order.Items);
```

كما رتبنا جميع الطلبات من الأحدث إلى الأقدم.

### لماذا نستخدم id مع userEmail؟

لو بحثنا باستخدام `id` فقط، يمكن لمستخدم تجربة أرقام مختلفة ورؤية طلب مستخدم آخر. الشرط المزدوج يجعل الطلب غير ظاهر إلا لصاحبه.

## 11. OrdersController

حمينا الـController:

```csharp
[Authorize(Roles = Roles.User)]
```

ويأخذ إيميل المستخدم من JWT:

```csharp
var userEmail =
    User.FindFirstValue(ClaimTypes.Email);
```

لذلك لا نسمح للعميل بإرسال بريد مختلف داخل Body ومحاولة إنشاء أو قراءة طلب باسم شخص آخر.

## 12. Endpoints النهائية

| Method | Endpoint | الوظيفة |
|---|---|---|
| `POST` | `/api/Orders` | إنشاء طلب |
| `GET` | `/api/Orders/delivery-methods` | عرض وسائل التوصيل |
| `GET` | `/api/Orders` | عرض طلبات المستخدم الحالي |
| `GET` | `/api/Orders/{id}` | عرض طلب محدد للمستخدم الحالي |

الاستجابات المهمة:

```text
200 = العملية نجحت
400 = تعذر إنشاء الطلب
401 = لا يوجد توكن صالح
403 = التوكن لا يحمل Role المطلوبة
404 = الطلب غير موجود أو لا يخص المستخدم
```

## 13. الاختبارات التي نجحت

### Create Order

أنشأنا طلبًا يحتوي منتجًا سعره `200` وكمية `2`:

```text
SubTotal = 200 × 2 = 400
Delivery = 10
Total    = 410
```

ظهرت الاستجابة الصحيحة مع العنوان ووسيلة التوصيل والعناصر.

### Delivery Methods

نجح:

```text
GET /api/Orders/delivery-methods → 200 OK
```

### Order Queries

نجحت الاختبارات:

```text
GET /api/Orders        → 200 وقائمة طلبات المستخدم
GET /api/Orders/1      → 200 والطلب المحدد
GET /api/Orders/999999 → 404
```

## 14. أخطاء مهمة وكيف نفهمها

### 401 Unauthorized

لا يوجد توكن صالح أو لم يتم إرساله.

### 403 Forbidden

التوكن صالح، لكن لا يحتوي Role باسم `User`.

التشبيه: الشخص يحمل بطاقة صحيحة، لكنها لا تمنحه تصريح دخول هذا القسم.

### Build ناجح لكن Mapping يفشل

تأكد من تسجيل:

```csharp
config.AddProfile<OrderProfile>();
```

### محاولة إضافة DeliveryMethod مرة أخرى

عند إنشاء الطلب، استخدم `DeliveryMethodId` للحفظ بدل ربط Entity غير متتبعة قبل `SaveChangesAsync`.

### الطلب يرجع دون Items أو DeliveryMethod

تأكد أن `OrderSpecifications` تحتوي:

```csharp
AddInclude(order => order.DeliveryMethod);
AddInclude(order => order.Items);
```

## 15. كيف تعيد بناء الجزء وحدك؟

اتبع هذا الترتيب:

```text
1. Domain Models
2. EF Configurations
3. DbSets
4. Migration + Database Update
5. Delivery Methods Seeding
6. Request/Response DTOs
7. IOrderService
8. OrderService
9. OrderProfile + Registration
10. Dependency Injection + ServiceManager
11. OrderSpecifications
12. OrdersController
13. Swagger Tests
```

لا تنتقل للخطوة التالية قبل التأكد من أن الحالية تعمل، وفرّق دائمًا بين:

```text
Build Success   = الكود يُترجم
Runtime Success = الطلب الحقيقي يعمل ويصل إلى Redis وSQL ويرجع النتيجة الصحيحة
```

## 16. الخلاصة السريعة

Session 07 حولت السلة المؤقتة إلى طلب رسمي آمن:

```text
Basket ID
→ Basket from Redis
→ Validate products and prices from SQL
→ Validate delivery method
→ Build OrderItems
→ Calculate totals
→ Save Order in SQL
→ Return OrderResponse
```

أهم ثلاث أفكار يجب تذكرها:

1. لا تثق في سعر قادم من العميل أو السلة؛ اقرأ السعر الرسمي من SQL.
2. اربط كل استعلام طلبات ببريد صاحب التوكن لمنع تسريب بيانات المستخدمين.
3. الـBuild وحده لا يكفي؛ اختبر المسار الحقيقي في Swagger.
