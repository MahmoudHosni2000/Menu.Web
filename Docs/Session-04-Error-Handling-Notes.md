# Session 04 — Error Handling وValidation وSwagger

هذا الملف مرجع عملي لما تم تنفيذه في Session 04 داخل مشروع `Menu.Web`، والهدف منه أن تتمكن من مراجعة الفكرة وإعادة تطبيقها لاحقًا دون مساعدة خارجية.

## 1. المشكلة التي حللناها

قبل Session 04 كانت الـ API تستطيع إرجاع البيانات، لكن عند حدوث مشكلة كان من الممكن أن:

- ترجع استجابة غير منظمة.
- تعرض تفاصيل داخلية لا ينبغي أن يراها العميل.
- ترجع `500` رغم أن المشكلة الحقيقية أن المنتج غير موجود.
- تختلف صيغة الأخطاء بين endpoints.
- لا يوضح Swagger الاستجابات الممكنة.

الهدف كان جعل الأخطاء منظمة، موحدة، آمنة، مسجلة في Logs، وموثقة في Swagger.

## 2. الحالات التي تعاملنا معها

| الحالة | Status Code | المسؤول عنها |
|---|---:|---|
| الطلب ناجح | `200` | Controller |
| Query Parameters غير صحيحة | `400` | Model Validation |
| المنتج غير موجود | `404` | Custom Exception + Middleware |
| عنوان endpoint غير موجود | `404` | StatusCodePages |
| خطأ برمجي غير متوقع | `500` | Global Exception Middleware |

مهم: ليس كل `404` له المصدر نفسه:

- `/api/Products/999999`: الـ endpoint موجود، لكن المنتج غير موجود.
- `/api/wrong-address`: عنوان الـ endpoint نفسه غير موجود.

## 3. الشكل الموحد للأخطاء — ErrorResponse

الملف: `Menu.Shared/Dtos/Errors/ErrorResponse.cs`

```csharp
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

دوره تحديد شكل موحد لأي خطأ:

```json
{
  "statusCode": 404,
  "message": "Product was not found."
}
```

وضعناه في `Menu.Shared` لأنه DTO مشترك قد تحتاجه طبقات Web وPresentation وتوثيق Swagger. وهو ليس Entity يتم حفظها في قاعدة البيانات.

## 4. ValidationErrorResponse

الملف: `Menu.Shared/Dtos/Errors/ValidationErrorResponse.cs`

```csharp
public class ValidationErrorResponse : ErrorResponse
{
    public IEnumerable<string> Errors { get; set; }
        = Enumerable.Empty<string>();
}
```

هي ترث `StatusCode` و`Message` من `ErrorResponse`، وتضيف قائمة `Errors` لأن الطلب قد يحتوي على أكثر من خطأ Validation.

مثال:

```json
{
  "statusCode": 400,
  "message": "Validation failed.",
  "errors": [
    "The field PageIndex must be between 1 and 2147483647."
  ]
}
```

## 5. قواعد Pagination Validation

الملف: `Menu.Shared/Dtos/Products/ProductQueryParameters.cs`

```csharp
[Range(1, int.MaxValue)]
public int PageIndex { get; set; } = 1;

[Range(1, 50)]
public int PageSize { get; set; } = 5;
```

المعنى:

- `PageIndex` يبدأ من `1`.
- `PageSize` يجب أن يكون بين `1` و`50`.
- القيم الافتراضية هي الصفحة الأولى و5 منتجات.

مثال طلب غير صالح:

```text
/api/Products?PageIndex=0
```

هذا الطلب يرجع `400` قبل دخول `GetAllProducts` لأن `[ApiController]` يشغّل Model Validation تلقائيًا.

## 6. تخصيص استجابة Validation في Program.cs

لم نحذف `AddControllers`، بل أضفنا إعدادات عليها:

```csharp
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .ToArray();

            var response = new ValidationErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Validation failed.",
                Errors = errors
            };

            return new BadRequestObjectResult(response);
        };
    });
```

`InvalidModelStateResponseFactory` معناها: عندما تفشل Model Validation، استخدم هذه الدالة لإنشاء الاستجابة بدل الشكل الافتراضي.

تفكيك جمع الأخطاء:

1. `ModelState.Values` يجلب نتائج فحص الخصائص.
2. `SelectMany` يجمع الأخطاء الداخلية في مجموعة واحدة.
3. `Select` يأخذ نص كل خطأ.
4. `ToArray` يحول النتيجة إلى Array.
5. `BadRequestObjectResult` يرسل HTTP `400` ومعه جسم الاستجابة.

## 7. ProductNotFoundException

الملف: `Core/Menu.Domain/Exceptions/ProductNotFoundException.cs`

```csharp
public class ProductNotFoundException : Exception
{
    public ProductNotFoundException(int productId)
        : base($"Product with ID {productId} was not found.")
    {
    }
}
```

بدل رمي `Exception` عامة، أنشأنا Exception تعبّر عن المشكلة بدقة.

السطر `: base(...)` يرسل الرسالة إلى constructor الخاص بالكلاس الأب `Exception`، ولذلك يمكن قراءتها لاحقًا من `exception.Message`.

## 8. رمي Exception من ProductService

الملف: `Menu.Services/Products/ProductService.cs`

```csharp
var product = await _unitOfWork
    .GetRepo<int, Product>()
    .GetByIdAsync(specifications);

if (product == null)
{
    throw new ProductNotFoundException(id);
}

return _mapper.Map<ProductDto>(product);
```

المسؤوليات هنا موزعة كالآتي:

- Repository تبحث في قاعدة البيانات.
- Service تفهم أن `null` معناها أن المنتج غير موجود وترمي Exception مناسبة.
- Service لا تنشئ HTTP response لأنها لا يفترض أن تعرف تفاصيل HTTP.
- Controller يظل نظيفًا ويعيد المنتج عند النجاح.

```csharp
var product = await _productService.GetProductByIdAsync(id);
return Ok(product);
```

## 9. GlobalExceptionHandlingMiddleware

الملف: `Menu.Web/Middlewares/GlobalExceptionHandlingMiddleware.cs`

وظيفتها مراقبة الطلبات والإمساك بأي Exception غير معالجة.

### RequestDelegate

```csharp
private readonly RequestDelegate _next;
```

يمثل الجزء التالي من HTTP Pipeline.

```csharp
await _next(context);
```

تعني: مرّر الطلب إلى الجزء التالي، مثل Controller.

### ILogger

```csharp
private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
```

يسجل تفاصيل المشكلة للمطور. المستخدم يحصل على رسالة آمنة، بينما المطور يحتفظ بالتفاصيل الحقيقية في Logs.

### try/catch

```csharp
try
{
    await _next(context);
}
catch (Exception exception)
{
    // Handle exception
}
```

لو حدث Exception في أي جزء يأتي بعد الـ Middleware، يرجع التنفيذ إلى `catch`.

## 10. تحويل Exception إلى Status Code

```csharp
var statusCode = exception is ProductNotFoundException
    ? StatusCodes.Status404NotFound
    : StatusCodes.Status500InternalServerError;
```

هذا هو ternary operator:

```csharp
condition ? valueWhenTrue : valueWhenFalse
```

- `ProductNotFoundException` تتحول إلى `404`.
- أي Exception أخرى تتحول إلى `500`.

ونحدد الرسالة:

```csharp
var message = exception is ProductNotFoundException
    ? exception.Message
    : "An unexpected error occurred.";
```

نخفي رسالة الأخطاء غير المتوقعة لأنها قد تحتوي على معلومات حساسة مثل أسماء الجداول أو مسارات الملفات أو تفاصيل قاعدة البيانات.

## 11. Logging Levels

### LogWarning

```csharp
_logger.LogWarning(
    "Product was not found: {Message}",
    exception.Message);
```

استخدمناه للمنتج غير الموجود؛ مشكلة متوقعة وليست انهيارًا في التطبيق.

### LogError

```csharp
_logger.LogError(
    exception,
    "An unhandled exception occurred.");
```

استخدمناه للأخطاء غير المتوقعة. تمرير `exception` يجعل الـ Logger يسجل نوع الخطأ والرسالة وStack Trace ومكان حدوثه.

القاعدة:

- حدث متوقع لكنه يستحق الانتباه: `Warning`.
- فشل غير متوقع في التطبيق: `Error`.

## 12. كتابة استجابة الخطأ

```csharp
context.Response.StatusCode = statusCode;
context.Response.ContentType = "application/json";

var errorResponse = new ErrorResponse
{
    StatusCode = statusCode,
    Message = message
};

await context.Response.WriteAsJsonAsync(errorResponse);
```

`WriteAsJsonAsync` تحول كائن C# إلى JSON وتكتبه في HTTP Response.

## 13. تسجيل الـ Middleware

في `Program.cs`:

```csharp
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
```

إنشاء الكلاس لا يكفي؛ يجب إضافته إلى HTTP Pipeline. الترتيب مهم، ولذلك تأتي قبل `app.MapControllers()` حتى تستطيع مراقبة أخطاء الـ Controllers والأجزاء التي تليها.

## 14. Product 404 مقابل Endpoint 404

### المنتج غير موجود

```text
/api/Products/999999
```

المسار:

```text
Controller
→ ProductService
→ ProductNotFoundException
→ GlobalExceptionHandlingMiddleware
→ JSON 404
```

### عنوان endpoint غير موجود

```text
/api/wrong-address
```

لا يوجد Controller أو Action يطابق العنوان، ولا يتم رمي `ProductNotFoundException`.

استخدمنا:

```csharp
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;

    if (response.StatusCode == StatusCodes.Status404NotFound)
    {
        var errorResponse = new ErrorResponse
        {
            StatusCode = StatusCodes.Status404NotFound,
            Message = "The requested resource was not found."
        };

        await response.WriteAsJsonAsync(errorResponse);
    }
});
```

المسار:

```text
لا يوجد endpoint مطابق
→ ASP.NET Core يحدد 404 بلا Body
→ StatusCodePages تكتب JSON
```

## 15. لماذا قيدنا StatusCodePages بـ 404؟

لأن StatusCodePages قد تعمل مع حالات فارغة أخرى، مثل `401` و`403`.

بدون الشرط قد نرسل رسالة `Not Found` عندما تكون المشكلة الحقيقية أن المستخدم غير مسجل أو غير مصرح له.

## 16. الفرق بين Middleware وStatusCodePages

| Global Middleware | StatusCodePages |
|---|---|
| تتعامل مع Exceptions | تتعامل مع Status Code بلا Body |
| تمسك `ProductNotFoundException` | تتعامل مع route غير موجود |
| تتعامل مع الأخطاء غير المتوقعة `500` | استخدمناها مع endpoint `404` |
| تعتمد على `try/catch` | لا تحتاج إلى Exception |

قاعدة سهلة:

> حدثت Exception؟ تعامل معها Middleware.  
> لم يوجد endpoint مطابق وخرجت استجابة 404 فارغة؟ تعامل معها StatusCodePages.

## 17. توثيق Swagger

الملف: `Menu.Presentation/ProductsController.cs`

### تحديد نوع المحتوى

```csharp
[Produces("application/json")]
```

وضعناه على مستوى Controller، لذلك يعرف Swagger أن endpoints الموجودة داخله تنتج JSON.

### GetAllProducts

```csharp
[ProducesResponseType(
    typeof(PaginatedResponse<ProductDto>),
    StatusCodes.Status200OK)]

[ProducesResponseType(
    typeof(ValidationErrorResponse),
    StatusCodes.Status400BadRequest)]
```

- النجاح يعيد صفحة منتجات.
- Query Parameters غير الصالحة تعيد Validation errors.

### GetProductById

```csharp
[ProducesResponseType(
    typeof(ProductDto),
    StatusCodes.Status200OK)]

[ProducesResponseType(
    typeof(ErrorResponse),
    StatusCodes.Status404NotFound)]
```

- النجاح يعيد منتجًا واحدًا.
- المنتج غير الموجود يعيد خطأ منظمًا.

### Brands وTypes

```csharp
typeof(IEnumerable<BrandDto>)
typeof(IEnumerable<TypeDto>)
```

استخدمنا `IEnumerable` لأن كل endpoint يعيد مجموعة وليس عنصرًا واحدًا.

## 18. Swagger Attributes لا تنفذ السلوك

هذه الـ attributes:

```csharp
[Produces]
[ProducesResponseType]
```

وظيفتها التوثيق. لا تمسك Exceptions ولا تحول `null` إلى `404`.

مثال:

```csharp
[ProducesResponseType(typeof(ErrorResponse), 404)]
```

يخبر Swagger أن `404` ممكنة، أما تنفيذ `404` الحقيقي فيحدث داخل الـ Middleware.

```text
Runtime behavior = الكود الفعلي
Swagger documentation = وصف السلوك
```

يجب أن يكون الاثنان متطابقين.

## 19. الاختبارات التي نفذناها

### نجاح 200

```text
GET /api/Products
GET /api/Products/{valid-id}
GET /api/Products/brands
GET /api/Products/types
```

### Validation 400

```text
GET /api/Products?PageIndex=0
```

النتيجة المتوقعة:

```json
{
  "errors": [
    "The field PageIndex must be between 1 and 2147483647."
  ],
  "statusCode": 400,
  "message": "Validation failed."
}
```

Swagger قد يمنع القيمة قبل إرسال الطلب، لذلك استخدمنا الرابط المباشر لاختبار السيرفر نفسه.

### Product 404

```text
GET /api/Products/999999
```

```json
{
  "statusCode": 404,
  "message": "Product with ID 999999 was not found."
}
```

### Endpoint 404

```text
GET /api/wrong-address
```

```json
{
  "statusCode": 404,
  "message": "The requested resource was not found."
}
```

### Unexpected 500

أنشأنا endpoint مؤقتًا يرمي:

```csharp
throw new Exception("Test exception.");
```

لكن العميل حصل على الرسالة الآمنة:

```json
{
  "statusCode": 500,
  "message": "An unexpected error occurred."
}
```

ثم حذفنا endpoint الاختبار حتى لا يبقى بابًا لافتعال أخطاء.

## 20. لماذا يتوقف Visual Studio عند throw؟

أثناء Debug قد يتوقف Visual Studio لحظة تنفيذ:

```csharp
throw new ProductNotFoundException(id);
```

هذا لا يعني أن الـ Middleware فشلت. الـ Debugger شاهد Exception لحظة رميها. بعد الضغط على **Continue** تكمل طريقها إلى الـ Middleware وتتحول إلى JSON.

## 21. كيف تضيف Custom Exception جديدة بنفسك؟

مثال: `OrderNotFound`.

1. حدد المشكلة بدقة.
2. أنشئ Exception باسم واضح، مثل `OrderNotFoundException`.
3. ارمِها من Service عندما تحدث المشكلة.
4. اجعل Global Middleware تتعرف على نوعها.
5. اربطها بالـ Status Code المناسب.
6. سجلها بمستوى Logging مناسب.
7. أرسل `ErrorResponse` منظمًا.
8. وثق الاستجابة في Swagger.
9. اختبر الحالة فعليًا.

لا تضع `try/catch` في كل Controller؛ الهدف من Global Middleware هو معالجة الأخطاء مركزيًا.

## 22. الخريطة الذهنية

```text
HTTP Request
    ↓
GlobalExceptionHandlingMiddleware
    ↓
StatusCodePages / باقي Pipeline
    ↓
Controller
    ↓
Service
    ↓
Repository / Database
```

عند النجاح:

```text
Controller → 200 JSON
```

عند فشل Validation:

```text
ApiController Validation
→ InvalidModelStateResponseFactory
→ 400 JSON
```

عند عدم وجود المنتج:

```text
Service throws ProductNotFoundException
→ Middleware
→ LogWarning
→ 404 JSON
```

عند حدوث خطأ غير متوقع:

```text
Exception
→ Middleware
→ LogError + Stack Trace
→ 500 JSON آمن
```

عند عدم وجود العنوان:

```text
Routing produces empty 404
→ StatusCodePages
→ 404 JSON
```

## 23. أسئلة مراجعة ذاتية

إذا استطعت الإجابة عن الأسئلة التالية فأنت فاهم Session 04:

1. لماذا لا نرسل تفاصيل Exception الحقيقية للمستخدم؟
2. ما الفرق بين `ErrorResponse` و`ValidationErrorResponse`؟
3. لماذا نرمي `ProductNotFoundException` من Service؟
4. من يحول `ProductNotFoundException` إلى `404`؟
5. ما الفرق بين Product 404 وEndpoint 404؟
6. لماذا قيدنا StatusCodePages بحالة `404`؟
7. ما الفرق بين `LogWarning` و`LogError`؟
8. هل `ProducesResponseType` تنفذ الخطأ أم توثقه فقط؟
9. لماذا اختبرنا Validation بالرابط المباشر؟
10. لماذا حذفنا `test-error` بعد اختبار `500`؟

## الخلاصة

Session 04 جعلت الـ API تتعامل مع الأخطاء في مكان مركزي، وترجع JSON موحدًا وآمنًا، وتسجل التفاصيل للمطور، وتوثق النتائج للعميل داخل Swagger.
