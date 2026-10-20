# Session 05 — Redis وBasket API

هذا الملف مرجع عملي لكل ما تم تنفيذه في Session 05 داخل مشروع `Menu.Web`، والهدف منه أن تفهم رحلة السلة كاملة وتستطيع إعادة تنفيذها واختبارها لاحقًا دون مساعدة خارجية.

## 1. ماذا بنينا؟

بنينا Basket API تستطيع:

- إنشاء سلة جديدة أو تحديث سلة موجودة.
- استرجاع السلة باستخدام `basketId`.
- حذف السلة.
- رفض البيانات غير الصحيحة قبل حفظها.
- حذف السلة تلقائيًا من Redis بعد 30 يومًا.
- توثيق الاستجابات داخل Swagger.

المسار الكامل للطلب:

```text
Client / Swagger
→ BasketController
→ BasketService
→ BasketRepository
→ Redis
```

## 2. الفكرة بتشبيه بسيط

تخيل متجرًا فيه عربة تسوق لكل عميل:

| الجزء التقني | التشبيه |
|---|---|
| `basketId` | رقم العربة |
| `BasketItem` | منتج موضوع داخل العربة |
| Redis | غرفة سريعة لحفظ العربات مؤقتًا |
| Repository | عامل المخزن الذي يقرأ ويحفظ ويحذف |
| Service | المشرف الذي ينظم العملية ويحوّل البيانات |
| Controller | موظف الاستقبال الذي يستقبل طلب العميل |
| DTO | الاستمارة التي تدخل وتخرج من الـAPI |
| TTL | تاريخ صلاحية العربة |

استخدمنا Redis لأن السلة بيانات مؤقتة وتحتاج إلى قراءة وكتابة سريعة، ولا نريد أن تبقى السلال المهجورة إلى الأبد.

## 3. ما هو Redis؟

Redis مخزن بيانات سريع يحتفظ بالبيانات في الذاكرة، ويخزنها في صورة `Key → Value`.

مثال:

```text
Key   = basket-test-1
Value = JSON يحتوي بيانات السلة ومنتجاتها
```

هو لا يكافئ مكونًا محددًا في الـFrontend. إنه خدمة مستقلة في الـBackend تشبه مخزنًا سريعًا يمكن للـAPI استخدامه للكاش أو السلال أو الجلسات.

## 4. لماذا استخدمنا WSL وDocker؟

### WSL 2

يسمح بتشغيل بيئة Linux داخل Windows. استخدمه Docker Desktop لتشغيل Linux containers.

### Docker

يشغّل Redis داخل بيئة معزولة اسمها Container بدل تثبيت Redis يدويًا داخل Windows.

التشبيه:

```text
Redis Image     = قالب جاهز للجهاز
Redis Container = الجهاز الذي شغّلناه من القالب
Docker          = المكان الذي يدير هذا الجهاز
WSL 2           = البيئة التي تسمح له بالعمل على Windows
```

## 5. تجهيز Redis باستخدام Docker

نزّلنا Redis Image:

```powershell
docker pull redis:7.4-alpine
```

- `redis`: اسم الـImage.
- `7.4`: إصدار Redis.
- `alpine`: نسخة Linux صغيرة الحجم.

ثم شغّلنا Container:

```powershell
docker run --name menu-redis -p 6379:6379 -d redis:7.4-alpine
```

تفكيك الأمر:

| الجزء | معناه |
|---|---|
| `docker run` | أنشئ وشغّل Container |
| `--name menu-redis` | اسم الـContainer |
| `-p 6379:6379` | اربط بورت الجهاز ببورت Redis |
| `-d` | شغّله في الخلفية |
| `redis:7.4-alpine` | الـImage المستخدمة |

اختبرنا الاتصال:

```powershell
docker exec -it menu-redis redis-cli ping
```

والنتيجة:

```text
PONG
```

`PONG` تعني أن Redis يعمل ويستقبل الأوامر.

## 6. أوامر Redis الأساسية التي جرّبناها

### تخزين قيمة

```powershell
docker exec -it menu-redis redis-cli SET course session-05
```

النتيجة `OK` تعني أن القيمة حُفظت.

### قراءة القيمة

```powershell
docker exec -it menu-redis redis-cli GET course
```

النتيجة:

```text
session-05
```

### إضافة مدة صلاحية

```powershell
docker exec -it menu-redis redis-cli EXPIRE course 60
```

هذا يجعل Redis يحذف المفتاح بعد 60 ثانية.

### معرفة الوقت المتبقي

```powershell
docker exec -it menu-redis redis-cli TTL course
```

نتائج `TTL` المهمة:

- رقم موجب: عدد الثواني المتبقية.
- `-1`: المفتاح موجود لكن بلا مدة صلاحية.
- `-2`: المفتاح غير موجود.

## 7. Basket Domain Models

### BasketItem

الملف: `Core/Menu.Domain/Entities/Baskets/BasketItem.cs`

```csharp
public class BasketItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string PictureUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
```

يمثل منتجًا واحدًا داخل السلة.

### CustomerBasket

الملف: `Core/Menu.Domain/Entities/Baskets/CustomerBasket.cs`

```csharp
public class CustomerBasket
{
    public string Id { get; set; } = string.Empty;
    public List<BasketItem> Items { get; set; } = new();
}
```

- `Id`: المفتاح الذي نستخدمه للوصول إلى السلة في Redis.
- `Items`: قائمة المنتجات الموجودة داخل السلة.

## 8. IBasketRepository

الملف: `Core/Menu.Domain/Contracts/IBasketRepository.cs`

```csharp
public interface IBasketRepository
{
    Task<CustomerBasket?> GetBasketAsync(string basketId);
    Task<CustomerBasket?> UpdateBasketAsync(CustomerBasket basket);
    Task<bool> DeleteBasketAsync(string basketId);
}
```

الـInterface هي عقد يحدد العمليات المطلوبة، لكنها لا تعرف كيف ينفذ Redis هذه العمليات.

التشبيه: الإدارة كتبت للعامل قائمة واجبات: استرجاع، حفظ، حذف. أما طريقة تنفيذها فهي داخل `BasketRepository`.

## 9. تثبيت StackExchange.Redis

أضفنا الحزمة إلى مشروع `Menu.Persistence`:

```xml
<PackageReference Include="StackExchange.Redis" Version="3.1.31" />
```

هذه المكتبة هي الوسيط الذي يسمح لكود C# بالتحدث مع Redis.

## 10. Redis Connection String

في `Menu.Web/appsettings.json`:

```json
"ConnectionStrings": {
  "RedisConnection": "localhost:6379"
}
```

- `localhost`: Redis يعمل على نفس الجهاز.
- `6379`: البورت الافتراضي لـRedis.

## 11. تسجيل اتصال Redis

في `Program.cs`:

```csharp
var redisConnectionString = builder.Configuration
    .GetConnectionString("RedisConnection")
    ?? throw new InvalidOperationException(
        "Redis connection string is not configured.");

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));
```

### لماذا Singleton؟

`IConnectionMultiplexer` مصمم ليكون اتصالًا طويل العمر يعاد استخدامه. إنشاء اتصال Redis جديد مع كل Request مكلف وغير مطلوب.

التشبيه: نفتح خط اتصال رئيسيًا واحدًا مع المخزن، ثم تستخدمه الطلبات بدل إجراء مكالمة جديدة من الصفر كل مرة.

## 12. BasketRepository

الملف: `Infrastructure/Menu.Persistence/Repos/BasketRepository.cs`

```csharp
public class BasketRepository(
    IConnectionMultiplexer connection) : IBasketRepository
{
    private readonly IDatabase _database = connection.GetDatabase();
}
```

`IConnectionMultiplexer` هو الاتصال العام، و`GetDatabase()` يعطينا الكائن الذي ننفذ من خلاله أوامر Redis.

### GetBasketAsync

```csharp
var basketJson = await _database.StringGetAsync(basketId);

return basketJson.IsNullOrEmpty
    ? null
    : JsonSerializer.Deserialize<CustomerBasket>(
        basketJson.ToString());
```

الخطوات:

1. استخدم `basketId` كمفتاح.
2. اقرأ JSON من Redis.
3. لو لم توجد قيمة، أعد `null`.
4. لو وُجدت، حوّل JSON إلى `CustomerBasket`.

### UpdateBasketAsync

```csharp
var basketJson = JsonSerializer.Serialize(basket);

var isSaved = await _database.StringSetAsync(
    basket.Id,
    basketJson,
    TimeSpan.FromDays(30));

return isSaved ? basket : null;
```

الخطوات:

1. حوّل `CustomerBasket` إلى JSON.
2. استخدم `basket.Id` كمفتاح.
3. احفظ JSON كقيمة.
4. أضف مدة صلاحية 30 يومًا.
5. أعد السلة عند النجاح أو `null` عند الفشل.

استخدمنا كلمة `Update` لأن `StringSetAsync` تنشئ المفتاح إن لم يكن موجودًا، وتستبدل قيمته إن كان موجودًا.

### DeleteBasketAsync

```csharp
return await _database.KeyDeleteAsync(basketId);
```

نستخدم `KeyDeleteAsync` لأن Redis يحذف العنصر كاملًا عن طريق المفتاح. القيمة `basketJson` ليست عنصرًا مستقلًا؛ هي المحتوى المرتبط بالمفتاح.

التشبيه: حذف درج من خزانة باستخدام رقمه يحذف الدرج بما يحتويه، ولا نحتاج إلى حذف كل شيء داخله منفصلًا.

## 13. لماذا خزّنا السلة كـJSON؟

Redis يستطيع تخزين String بسهولة، بينما `CustomerBasket` كائن C# مركب.

```text
CustomerBasket
→ Serialize
→ JSON String
→ Redis
```

وعند القراءة:

```text
Redis JSON String
→ Deserialize
→ CustomerBasket
```

- `Serialize`: تحويل كائن إلى نص JSON.
- `Deserialize`: إعادة نص JSON إلى كائن.

## 14. تسجيل BasketRepository

في `Program.cs`:

```csharp
builder.Services.AddScoped<IBasketRepository, BasketRepository>();
```

المعنى: عندما يطلب كلاس `IBasketRepository`، أنشئ له `BasketRepository`.

استخدمنا `Scoped` لكي يحصل كل HTTP Request على instance خاص به خلال الطلب.

## 15. Basket DTOs والـValidation

الـDTO هي شكل البيانات التي يستقبلها أو يعيدها الـAPI. فصلناها عن Domain Models حتى لا نكشف الكائنات الداخلية مباشرة.

### BasketDto

الملف: `Menu.Shared/Dtos/Baskets/BasketDto.cs`

```csharp
public class BasketDto
{
    [Required(ErrorMessage = "Basket id is required.")]
    public string Id { get; set; } = string.Empty;

    public List<BasketItemDto> Items { get; set; } = new();
}
```

`[Required]` تمنع إرسال السلة من دون `Id`.

### BasketItemDto

الملف: `Menu.Shared/Dtos/Baskets/BasketItemDto.cs`

```csharp
[Range(
    1,
    int.MaxValue,
    ErrorMessage = "Product id must be greater than 0.")]
public int ProductId { get; set; }

[Range(
    1,
    int.MaxValue,
    ErrorMessage = "Quantity must be greater than 0.")]
public int Quantity { get; set; }
```

هذا يمنع:

- منتجًا برقم `0` أو رقم سالب.
- كمية `0` أو كمية سالبة.

بسبب `[ApiController]`، يفحص ASP.NET Core الـDTO تلقائيًا قبل تنفيذ Action، ويستخدم شكل `ValidationErrorResponse` الذي أنشأناه في Session 04.

## 16. BasketProfile وAutoMapper

الملف: `Menu.Services/Mapping/Baskets/BasketProfile.cs`

```csharp
public class BasketProfile : Profile
{
    public BasketProfile()
    {
        CreateMap<BasketDto, CustomerBasket>()
            .ReverseMap();

        CreateMap<BasketItemDto, BasketItem>()
            .ReverseMap();
    }
}
```

وظيفته تعريف قواعد التحويل بين DTOs وDomain Models.

```text
BasketDto ↔ CustomerBasket
BasketItemDto ↔ BasketItem
```

`ReverseMap()` تعني أن التحويل يعمل في الاتجاهين.

التشبيه: `BasketProfile` مثل مترجم لديه قاموس يوضح أن كل خانة في الاستمارة الخارجية تقابل أي خانة في النموذج الداخلي.

سجلناه في `Program.cs`:

```csharp
builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<ProductProfile>();
    config.AddProfile<BasketProfile>();
});
```

إنشاء Profile وحده لا يكفي؛ يجب تسجيله حتى يعرف AutoMapper بوجوده.

## 17. IBasketService

الملف: `Menu.Services.Abstraction/Baskets/IBasketService.cs`

```csharp
public interface IBasketService
{
    Task<BasketDto?> GetBasketAsync(string basketId);
    Task<BasketDto?> UpdateBasketAsync(BasketDto basket);
    Task<bool> DeleteBasketAsync(string basketId);
}
```

هذا عقد طبقة Service، ويستخدم DTOs لأن Controller يتعامل مع بيانات الـAPI، وليس Domain Models مباشرة.

## 18. BasketService

الملف: `Menu.Services/Baskets/BasketService.cs`

تعتمد على:

```csharp
IBasketRepository basketRepository,
IMapper mapper
```

- Repository تتواصل مع Redis.
- Mapper يحول بين DTO وDomain.

### GetBasketAsync

```csharp
var basket = await _basketRepository
    .GetBasketAsync(basketId);

return basket is null
    ? null
    : _mapper.Map<BasketDto>(basket);
```

تقرأ `CustomerBasket` من Repository، ثم تحولها إلى `BasketDto` قبل إعادتها إلى Controller.

### UpdateBasketAsync

```csharp
var customerBasket =
    _mapper.Map<CustomerBasket>(basket);

var savedBasket =
    await _basketRepository.UpdateBasketAsync(customerBasket);

return savedBasket is null
    ? null
    : _mapper.Map<BasketDto>(savedBasket);
```

المسار:

```text
BasketDto
→ CustomerBasket
→ Repository
→ Redis
→ CustomerBasket
→ BasketDto
```

### DeleteBasketAsync

```csharp
return await _basketRepository.DeleteBasketAsync(basketId);
```

لا يوجد تحويل هنا لأن العملية تستقبل `basketId` وتعيد `true` أو `false` فقط.

## 19. تسجيل BasketService

في `Program.cs`:

```csharp
builder.Services.AddScoped<IBasketService, BasketService>();
```

عندما يطلب التطبيق `IBasketService`، يوفر له `BasketService`.

## 20. ربط BasketService بـServiceManager

في `IServiceManager`:

```csharp
public interface IServiceManager
{
    IProductService ProductService { get; }
    IBasketService BasketService { get; }
}
```

وفي `ServiceManager`:

```csharp
public sealed class ServiceManager(
    IProductService productService,
    IBasketService basketService) : IServiceManager
{
    public IProductService ProductService { get; } = productService;
    public IBasketService BasketService { get; } = basketService;
}
```

`ServiceManager` يجمع الخدمات خلف مدخل واحد.

بدونه سيظل التطبيق يعمل، لكن كل Controller سيحقن الخدمة التي يحتاجها مباشرة، مثل:

```csharp
BasketController(IBasketService basketService)
```

إذن `ServiceManager` اختيار تنظيمي في هذا المشروع، وليس شرطًا لتشغيل ASP.NET Core.

## 21. BasketController

الملف: `Menu.Presentation/BasketController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BasketController(
    IServiceManager serviceManager) : ControllerBase
```

- `[ApiController]`: يفعّل سلوك API والـValidation التلقائي.
- `[Route("api/[controller]")]`: يجعل العنوان الأساسي `/api/Basket`.
- `[Produces("application/json")]`: يوثق أن النتائج JSON.
- `ControllerBase`: الأساس المناسب لـWeb API دون Views.

## 22. GET Basket endpoint

```csharp
[HttpGet("{basketId}")]
public async Task<ActionResult<BasketDto>> GetBasket(
    string basketId)
{
    var basket =
        await _basketService.GetBasketAsync(basketId);

    return Ok(basket ?? new BasketDto
    {
        Id = basketId
    });
}
```

مثال:

```text
GET /api/Basket/basket-test-1
```

- لو السلة موجودة: يعيدها.
- لو غير موجودة: يعيد سلة فارغة بنفس `basketId` وحالة `200`.

هذا يسهل على الـFrontend بدء العمل بسلة فارغة بدل التعامل مع خطأ.

## 23. POST Basket endpoint

```csharp
[HttpPost]
public async Task<ActionResult<BasketDto>> UpdateBasket(
    BasketDto basket)
{
    var updatedBasket =
        await _basketService.UpdateBasketAsync(basket);

    return updatedBasket is null
        ? BadRequest()
        : Ok(updatedBasket);
}
```

مثال Request:

```json
{
  "id": "basket-test-1",
  "items": [
    {
      "productId": 1,
      "productName": "Test Product",
      "pictureUrl": "images/products/test.png",
      "price": 100,
      "quantity": 2
    }
  ]
}
```

- لو الحفظ نجح: `200` مع السلة المحفوظة.
- لو فشل الحفظ: `400`.
- لو فشلت Data Annotations: `400 ValidationErrorResponse` قبل دخول Action.

## 24. DELETE Basket endpoint

```csharp
[HttpDelete("{basketId}")]
public async Task<IActionResult> DeleteBasket(
    string basketId)
{
    var isDeleted =
        await _basketService.DeleteBasketAsync(basketId);

    return isDeleted
        ? NoContent()
        : NotFound(new ErrorResponse
        {
            StatusCode = StatusCodes.Status404NotFound,
            Message = "The basket was not found."
        });
}
```

مثال:

```text
DELETE /api/Basket/basket-test-1
```

- السلة موجودة وتم حذفها: `204 No Content`.
- السلة غير موجودة: `404` مع `ErrorResponse` منظم.

لم نستخدم `StatusCodePages` لإنشاء هذا الخطأ، لأن Controller نفسه يعرف أن عملية الحذف فشلت ويكتب Body صراحة.

## 25. توثيق Swagger

استخدمنا `ProducesResponseType` لوصف الاستجابات المتوقعة.

### GET

```csharp
[ProducesResponseType(
    typeof(BasketDto),
    StatusCodes.Status200OK)]
```

### POST

```csharp
[ProducesResponseType(
    typeof(BasketDto),
    StatusCodes.Status200OK)]

[ProducesResponseType(
    typeof(ValidationErrorResponse),
    StatusCodes.Status400BadRequest)]
```

### DELETE

```csharp
[ProducesResponseType(
    StatusCodes.Status204NoContent)]

[ProducesResponseType(
    typeof(ErrorResponse),
    StatusCodes.Status404NotFound)]
```

هذه Attributes توثق السلوك في Swagger، لكنها لا تنفذه. التنفيذ الحقيقي موجود في كود الـActions والـValidation.

## 26. الاختبارات التي نفذناها

### Redis يعمل

```powershell
docker exec -it menu-redis redis-cli ping
```

النتيجة: `PONG`.

### GET لسلة غير موجودة

```text
GET /api/Basket/basket-test-1
```

النتيجة: `200` وسلة فارغة.

### POST وحفظ السلة

```text
POST /api/Basket
```

النتيجة: `200` والسلة المحفوظة.

### GET بعد الحفظ

أعاد نفس المنتجات التي حُفظت، وهذا أثبت أن القراءة و`Deserialize` يعملان.

### DELETE لسلة موجودة

النتيجة: `204 No Content`.

### GET بعد الحذف

أعاد سلة فارغة، وهذا أثبت أن المفتاح حُذف من Redis.

### DELETE لسلة غير موجودة

النتيجة النهائية:

```json
{
  "statusCode": 404,
  "message": "The basket was not found."
}
```

### Basket Id Validation

إرسال `id` فارغ أعاد `400 ValidationErrorResponse`.

### BasketItem Validation

إرسال:

```json
{
  "productId": 0,
  "quantity": 0
}
```

أعاد `400` ورسالتين:

```text
Product id must be greater than 0.
Quantity must be greater than 0.
```

### اختبار مدة الصلاحية

بعد حفظ سلة باسم `ttl-test` نفذنا:

```powershell
docker exec -it menu-redis redis-cli TTL ttl-test
```

ظهرت نتيجة موجبة قريبة من:

```text
2592000
```

لأن:

```text
30 يوم × 24 ساعة × 60 دقيقة × 60 ثانية
= 2,592,000 ثانية
```

ثم نظفنا بيانات الاختبار:

```powershell
docker exec -it menu-redis redis-cli DEL ttl-test
```

النتيجة `(integer) 1` تعني أن المفتاح وُجد وحُذف.

## 27. الفرق بين نجاح Build ونجاح الاختبار

نجاح:

```powershell
dotnet build Menu.Web.slnx --no-restore --configuration Release
```

يعني أن الكود يُترجم ولا يحتوي على Compile Errors.

لكنه وحده لا يثبت أن:

- Redis يعمل.
- الاتصال صحيح.
- JSON يُحفظ ويُقرأ.
- Endpoints تعيد Status Codes الصحيحة.
- مدة الصلاحية تعمل.

لذلك جمعنا بين Build واختبارات Swagger وأوامر Redis الفعلية.

المراجعة النهائية نجحت بـ:

```text
0 Warnings
0 Errors
```

## 28. الخريطة الذهنية الكاملة

### عند حفظ السلة

```text
POST /api/Basket
→ Model Validation
→ BasketController
→ BasketService
→ Map BasketDto to CustomerBasket
→ BasketRepository
→ Serialize to JSON
→ Redis StringSetAsync + 30-day TTL
→ Map result to BasketDto
→ 200 JSON
```

### عند قراءة السلة

```text
GET /api/Basket/{basketId}
→ BasketController
→ BasketService
→ BasketRepository
→ Redis StringGetAsync
→ Deserialize JSON
→ Map CustomerBasket to BasketDto
→ 200 JSON
```

### عند حذف السلة

```text
DELETE /api/Basket/{basketId}
→ BasketController
→ BasketService
→ BasketRepository
→ Redis KeyDeleteAsync
→ true  = 204
→ false = 404 ErrorResponse
```

### عند إرسال بيانات غير صحيحة

```text
POST /api/Basket
→ ApiController Model Validation
→ InvalidModelStateResponseFactory
→ 400 ValidationErrorResponse
→ لا تصل البيانات إلى Redis
```

## 29. كيف تعيد تشغيل Redis لاحقًا؟

تأكد أولًا أن Docker Desktop يعمل، ثم اعرض الـContainers:

```powershell
docker ps -a
```

إذا كان `menu-redis` متوقفًا:

```powershell
docker start menu-redis
```

اختبره:

```powershell
docker exec -it menu-redis redis-cli ping
```

إذا ظهرت `PONG` فهو جاهز.

لإيقافه:

```powershell
docker stop menu-redis
```

إيقاف Container لا يحذفه. لا تستخدم `docker rm` إلا إذا أردت حذف الـContainer نفسه.

## 30. أسئلة مراجعة ذاتية

إذا استطعت الإجابة عن هذه الأسئلة فأنت فاهم Session 05:

1. لماذا اخترنا Redis للسلة بدل التعامل معها كبيانات دائمة عادية؟
2. ما الفرق بين Redis Image وContainer؟
3. ماذا يعني الربط `6379:6379`؟
4. ما وظيفة `IConnectionMultiplexer`؟ ولماذا سجلناه Singleton؟
5. لماذا نحول `CustomerBasket` إلى JSON قبل الحفظ؟
6. ما الفرق بين `Serialize` و`Deserialize`؟
7. لماذا يستخدم `GetBasketAsync` المفتاح `basketId`؟
8. لماذا اسم الحذف `KeyDeleteAsync` وليس `StringDeleteAsync`؟
9. ما فائدة `TimeSpan.FromDays(30)`؟
10. ما معنى نتائج TTL الموجبة و`-1` و`-2`؟
11. ما الفرق بين `BasketDto` و`CustomerBasket`؟
12. ما وظيفة `BasketProfile`؟
13. ما مسؤولية كل من Controller وService وRepository؟
14. هل يستطيع المشروع العمل دون `ServiceManager`؟
15. لماذا يعيد GET سلة فارغة عندما لا يجد المفتاح؟
16. ما الفرق بين DELETE `204` وDELETE `404`؟
17. هل `ProducesResponseType` تنفذ الاستجابة أم توثقها فقط؟
18. لماذا لا يكفي نجاح Build لإثبات أن Basket API تعمل؟

## خلاصة جزء Basket

في Session 05 أنشأنا Basket API كاملة تعتمد على Redis كمخزن سريع ومؤقت. فصلنا المسؤوليات بين Controller وService وRepository، واستخدمنا AutoMapper للتحويل، وData Annotations لمنع البيانات غير الصالحة، وحددنا عمر السلة بـ30 يومًا.

لم نكتفِ بأن الكود يعمل في Build؛ اختبرنا الحفظ والقراءة والحذف والـValidation واستجابات الأخطاء ومدة الصلاحية فعليًا.

---

# الجزء الثاني — Redis Response Cache

## 31. لماذا أضفنا Cache؟

بعض طلبات القراءة، مثل جلب المنتجات، قد تتكرر كثيرًا بنفس الفلاتر والصفحة. بدون Cache ينفذ التطبيق نفس الاستعلام في SQL Server كل مرة.

مع الـCache:

```text
أول طلب
→ Controller
→ Service
→ SQL Server
→ حفظ النتيجة في Redis

نفس الطلب مرة أخرى
→ Redis مباشرة
→ لا يعمل Controller ولا SQL Server
```

التشبيه: بدل إعداد نفس الوجبة من البداية لكل عميل، يحتفظ المطعم بنسخة جاهزة لمدة قصيرة.

الفرق عن Basket:

| Basket | Response Cache |
|---|---|
| بيانات العميل نفسها | نسخة مؤقتة من نتيجة API |
| Redis هي مخزن السلة | البيانات الأصلية موجودة في SQL Server |
| مدة الحفظ 30 يومًا | طبقنا مدة 300 ثانية |

## 32. ICacheRepository

الملف: `Core/Menu.Domain/Contracts/ICacheRepository.cs`

```csharp
public interface ICacheRepository
{
    Task<string?> GetAsync(string cacheKey);

    Task SetAsync(
        string cacheKey,
        object response,
        TimeSpan timeToLive);
}
```

- `GetAsync`: تقرأ JSON محفوظًا باستخدام المفتاح، أو تعيد `null`.
- `SetAsync`: تحفظ نتيجة لمدة محددة.

هذا مجرد عقد؛ لا يحتوي على أوامر Redis الفعلية.

## 33. CacheRepository

الملف: `Infrastructure/Menu.Persistence/Repos/CacheRepository.cs`

تستخدم اتصال Redis المسجل مسبقًا:

```csharp
public class CacheRepository(
    IConnectionMultiplexer connection) : ICacheRepository
{
    private readonly IDatabase _database =
        connection.GetDatabase();
}
```

### قراءة Cache

```csharp
var cachedResponse =
    await _database.StringGetAsync(cacheKey);

return cachedResponse.IsNullOrEmpty
    ? null
    : cachedResponse.ToString();
```

### حفظ Cache

```csharp
var serializedResponse = JsonSerializer.Serialize(
    response,
    new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });

await _database.StringSetAsync(
    cacheKey,
    serializedResponse,
    timeToLive);
```

الخطوات:

1. حوّل كائن C# إلى JSON.
2. استخدم `camelCase` لأسماء خصائص JSON.
3. احفظ JSON تحت `cacheKey`.
4. أرسل مدة الصلاحية إلى Redis.

التشبيه: غلف النتيجة، اكتب اسم الطلب على العبوة، ثم ضع تاريخ الصلاحية.

## 34. تسجيل CacheRepository

في `Program.cs`:

```csharp
builder.Services.AddScoped<ICacheRepository, CacheRepository>();
```

عندما يطلب التطبيق `ICacheRepository`، تنشئ Dependency Injection كائنًا من `CacheRepository`.

## 35. ICacheService وCacheService

العقد موجود في:

`Menu.Services.Abstraction/Cache/ICacheService.cs`

والتنفيذ في:

`Menu.Services/Cache/CacheService.cs`

تمرر Service العمليات إلى Repository:

```csharp
public Task<string?> GetAsync(string cacheKey)
{
    return _cacheRepository.GetAsync(cacheKey);
}

public Task SetAsync(
    string cacheKey,
    object response,
    TimeSpan timeToLive)
{
    return _cacheRepository.SetAsync(
        cacheKey,
        response,
        timeToLive);
}
```

المسار:

```text
Presentation
→ ICacheService
→ CacheService
→ ICacheRepository
→ CacheRepository
→ Redis
```

وسجلنا الخدمة:

```csharp
builder.Services.AddScoped<ICacheService, CacheService>();
```

## 36. CachedAttribute

الملف: `Menu.Presentation/Attributes/CachedAttribute.cs`

هي فلتر يقف قبل وبعد Controller Action.

```csharp
public class CachedAttribute(
    int timeToLiveSeconds) : Attribute, IAsyncActionFilter
```

- `Attribute`: تسمح باستخدام `[Cached(300)]`.
- `timeToLiveSeconds`: مدة التخزين التي نرسلها من الـController.
- `IAsyncActionFilter`: يسمح بتنفيذ كود قبل وبعد Action.

## 37. الحصول على CacheService داخل Attribute

```csharp
var cacheService =
    context.HttpContext.RequestServices
        .GetRequiredService<ICacheService>();
```

`RequestServices` هي الخدمات المسجلة للطلب الحالي. و`GetRequiredService` تجلب `CacheService`، أو ترمي خطأ واضحًا إذا نسينا تسجيلها.

## 38. إنشاء Cache Key

```csharp
var request = context.HttpContext.Request;

var cacheKey =
    $"{request.Path}{request.QueryString}";
```

استخدمنا المسار مع Query String حتى يكون لكل طلب مختلف مفتاح مستقل:

```text
/api/Products?PageIndex=1&PageSize=5
/api/Products?PageIndex=2&PageSize=5
```

لو استخدمنا المسار فقط، قد يحصل طلب الصفحة الثانية على نتيجة الصفحة الأولى بالخطأ.

## 39. Cache Hit

```csharp
var cachedResponse =
    await cacheService.GetAsync(cacheKey);

if (!string.IsNullOrEmpty(cachedResponse))
{
    context.Result = new ContentResult
    {
        Content = cachedResponse,
        ContentType = "application/json",
        StatusCode = StatusCodes.Status200OK
    };

    return;
}
```

إذا وجدنا JSON في Redis:

1. نضعه مباشرة في HTTP Response.
2. نحدد نوع المحتوى `application/json`.
3. نعيد `200`.
4. ننفذ `return`، ولذلك لا تعمل Controller Action.

التشبيه: الحارس وجد الوجبة جاهزة، فسلّمها للعميل ولم يرسل الطلب إلى المطبخ.

## 40. Cache Miss

```csharp
var executedContext = await next();
```

لو لم نجد Cache، تسمح `next()` بتشغيل Controller Action. ونحتفظ بالنتيجة داخل `executedContext`.

ثم نحفظ النتائج الناجحة فقط:

```csharp
if (executedContext.Result is OkObjectResult okResult &&
    okResult.Value is not null)
{
    await cacheService.SetAsync(
        cacheKey,
        okResult.Value,
        TimeSpan.FromSeconds(_timeToLiveSeconds));
}
```

لا نحفظ `400` أو `404`، ولا نحفظ نتيجة فارغة. نحفظ فقط `Ok(...)` التي تحتوي بيانات.

## 41. تطبيق Cache على Products

في `ProductsController`:

```csharp
[HttpGet]
[Cached(300)]
public async Task<IActionResult> GetAllProducts(
    [FromQuery] ProductQueryParameters queryParameters)
```

`300` تعني الاحتفاظ بكل نتيجة لمدة خمس دقائق.

كل Query String تحصل على مفتاح ونتيجة منفصلين.

## 42. اختبارات Cache التي نفذناها

### Cache Miss

1. حذفنا المفتاح من Redis.
2. وضعنا Breakpoint داخل `GetAllProducts`.
3. أرسلنا الطلب.
4. توقفت Visual Studio عند Breakpoint.

هذا أثبت أن Controller وSQL عملا في الطلب الأول.

### التأكد من الحفظ

```powershell
docker exec -it menu-redis redis-cli --scan --pattern "*Products*"
```

ظهر مفتاح الطلب داخل Redis.

### التأكد من TTL

```powershell
docker exec -it menu-redis redis-cli TTL "/api/Products?PageIndex=1&PageSize=5"
```

ظهرت قيمة موجبة أقل من أو تساوي 300 ثانية.

### Cache Hit

أرسلنا نفس الطلب مرة أخرى، فظهرت النتيجة دون توقف Visual Studio عند Breakpoint. هذا أثبت أن Attribute أعادت JSON من Redis ولم تدخل Controller.

### فصل الصفحات

اختبرنا صفحتين مختلفتين، وظهر مفتاحان:

```text
/api/Products?PageIndex=1&PageSize=5
/api/Products?PageIndex=2&PageSize=5
```

هذا أثبت أن الصفحة الأولى والثانية لا تشتركان في نتيجة Cache واحدة.

ثم حذفنا مفاتيح الاختبار، وأكد الفحص النهائي عدم وجود مفاتيح Products متبقية.

## 43. الخريطة الذهنية للـCache

```text
GET /api/Products?...query
        ↓
CachedAttribute
        ↓
إنشاء Key من Path + QueryString
        ↓
CacheService.GetAsync
        ↓
CacheRepository.GetAsync
        ↓
Redis
```

إذا وجدت النتيجة:

```text
Redis JSON
→ ContentResult 200
→ return
→ Controller لا تعمل
```

إذا لم تجد النتيجة:

```text
next()
→ ProductsController
→ ProductService
→ SQL Server
→ Ok(products)
→ CacheService.SetAsync
→ Redis لمدة 300 ثانية
```

## 44. أسئلة مراجعة ذاتية للـCache

1. ما الفرق بين Basket داخل Redis وResponse Cache؟
2. لماذا نحول نتيجة الـAPI إلى JSON قبل التخزين؟
3. ما وظيفة `cacheKey`؟
4. لماذا أضفنا Query String إلى المفتاح؟
5. ما معنى Cache Hit؟
6. ما معنى Cache Miss؟
7. لماذا يمنع `return` تنفيذ Controller؟
8. ما وظيفة `next()` داخل Action Filter؟
9. لماذا نحفظ `OkObjectResult` فقط؟
10. ماذا تعني `[Cached(300)]`؟
11. كيف أثبت الـBreakpoint أن الطلب الثاني جاء من Redis؟
12. لماذا لا يكفي نجاح Build لإثبات أن الـCache تعمل؟

## الخلاصة النهائية لـSession 05

في Session 05 استخدمنا Redis بطريقتين مختلفتين:

1. مخزن فعلي ومؤقت لسلة العميل لمدة 30 يومًا.
2. Cache لنتائج Products لمدة 300 ثانية لتقليل استعلامات SQL وتسريع الطلبات المتكررة.

طبّقنا الفصل بين Controller وService وRepository، واختبرنا Basket وCache عمليًا داخل Swagger وVisual Studio وRedis، ثم أنهينا المراجعة بـBuild ناجح دون Errors أو Warnings.
