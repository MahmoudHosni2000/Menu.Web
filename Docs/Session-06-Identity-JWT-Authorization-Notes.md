# Session 06 — Identity وJWT وRole-Based Authorization

هذا الملف مرجع عملي لما تم تنفيذه في Session 06 داخل مشروع `Menu.Web`. الهدف: فهم تسجيل المستخدم، تسجيل الدخول، إنشاء JWT Token، وحماية الـ endpoints بالأدوار بدون الرجوع إلى فيديوهات.

## 1. المشكلة التي حللناها

قبل هذه الجلسة كان أي شخص يستطيع استدعاء Basket endpoint. لم يكن التطبيق يعرف:

- من هو المستخدم.
- هل كلمة المرور صحيحة.
- هل المستخدم يملك صلاحية دخول الـ Basket.

بنينا نظامًا يعرّف المستخدم، يتحقق من بياناته، ويعطيه Token مؤقتًا يحمل دوره.

تشبيه سريع: التطبيق مثل مطعم.

- `Identity` = موظف الاستقبال الذي يسجل بيانات العميل وكلمة المرور بأمان.
- `JWT Token` = بطاقة دخول مؤقتة يحملها العميل بعد تسجيل الدخول.
- `Role` = نوع التصريح المكتوب على البطاقة، مثل `User` أو `Admin`.
- `[Authorize]` = الحارس على باب القسم المحمي.

## 2. الصورة الكاملة

```text
Register أو Login
        ↓
AuthController
        ↓
AuthService
        ↓
UserManager + ASP.NET Core Identity
        ↓
SQL Server (AspNetUsers / AspNetRoles / العلاقات)
        ↓
JWT Token يحتوي Name + Email + Role
        ↓
العميل يرسل Token في Authorization: Bearer ...
        ↓
UseAuthentication يفحصه
        ↓
[Authorize(Roles = Roles.User)] يسمح أو يمنع Basket
```

## 3. ASP.NET Core Identity

الحزمة المستخدمة:

```text
Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

هي نظام جاهز لإدارة المستخدمين وكلمات المرور والأدوار. لا نخزن كلمة المرور كنص عادي؛ Identity تحولها إلى hash آمن وتدير التحقق منه.

### ApplicationUser

الملف: `Infrastructure/Menu.Persistence/Data/Identity/ApplicationUser.cs`

```csharp
public class ApplicationUser : IdentityUser
{
}
```

`IdentityUser` هو المستخدم الجاهز من Microsoft ويحتوي على خصائص مثل `UserName` و`Email` و`PasswordHash`.

أنشأنا `ApplicationUser` لكي يكون لدينا مكان نضيف فيه بيانات خاصة بالتطبيق مستقبلًا، مثل الاسم الكامل أو العنوان، بدل تغيير كود Microsoft نفسه.

### ربط Identity بقاعدة البيانات

`StoreDbContext` يرث من:

```csharp
IdentityDbContext<ApplicationUser>
```

لذلك أصبح Entity Framework يعرف جداول Identity. بعد إنشاء وتطبيق Migration ظهرت جداول مثل:

- `AspNetUsers`: المستخدمون.
- `AspNetRoles`: الأدوار.
- `AspNetUserRoles`: يربط المستخدم بدوره.

## 4. تسجيل Identity في Program.cs

```csharp
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<StoreDbContext>();
```

المعنى:

- `AddIdentity` يسجل خدمة إدارة المستخدمين والأدوار.
- `ApplicationUser` هو شكل المستخدم في مشروعنا.
- `IdentityRole` هو شكل الدور، مثل `User` و`Admin`.
- `AddEntityFrameworkStores` يقول لـ Identity: احفظ البيانات في SQL Server من خلال `StoreDbContext`.

## 5. DTOs الخاصة بالمستخدم

مكانها: `Menu.Shared/Dtos/Identity`، لأنها بيانات تنتقل بين الـ API والعميل وليست Entities قاعدة بيانات.

### UserRegisterDto

```csharp
public class UserRegisterDto
{
    [Required] public string UserName { get; set; }
    [Required, EmailAddress] public string Email { get; set; }
    [Required, MinLength(6)] public string Password { get; set; }
    [Phone] public string? PhoneNumber { get; set; }
}
```

يمثل البيانات القادمة من المستخدم عند التسجيل. الـ Validation تمنع الطلب غير الصحيح قبل دخوله إلى `AuthService`.

### UserLoginDto

يطلب فقط `Email` و`Password` لأن المستخدم موجود بالفعل.

### UserDto

```csharp
public class UserDto
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Token { get; set; }
}
```

هذه هي البيانات الآمنة التي نعيدها للعميل. لا نعيد كلمة المرور أو `PasswordHash` إطلاقًا.

## 6. IAuthService وAuthService

العقد: `Menu.Services.Abstraction/Identity/IAuthService.cs`

```csharp
Task<UserDto> RegisterAsync(UserRegisterDto registerDto);
Task<UserDto> LoginAsync(UserLoginDto loginDto);
```

والتنفيذ: `Infrastructure/Menu.Persistence/Identity/AuthService.cs`.

`IAuthService` مثل وصف الخدمة في قائمة المطعم، و`AuthService` هو المطبخ الذي ينفذها. الـ Controller يتعامل مع العقد فقط ولا يهتم بتفاصيل Identity.

### UserManager<ApplicationUser>

```csharp
private readonly UserManager<ApplicationUser> _userManager;
```

هذه خدمة من Identity. هي المسؤولة عن إنشاء المستخدم، حفظ كلمة المرور بشكل آمن، البحث بالإيميل، وفحص كلمة المرور وإسناد الأدوار.

## 7. RegisterAsync — تسجيل مستخدم

المسار:

```text
POST /api/Auth/register
→ AuthController
→ AuthService.RegisterAsync
→ UserManager.CreateAsync
→ UserManager.AddToRoleAsync(User)
→ CreateToken
→ UserDto
```

أهم الخطوات:

1. ننشئ `ApplicationUser` من `UserRegisterDto`.
2. نستدعي `CreateAsync(user, password)`.
3. Identity يعمل hash لكلمة المرور ثم يحفظ المستخدم.
4. إذا فشلت العملية، نجمع رسائل Identity ونرمي `UserRegistrationException`.
5. إذا نجحت، نعطيه الدور الافتراضي `User`.
6. ننشئ Token ونرجعه مع اسم المستخدم والإيميل.

```csharp
await _userManager.AddToRoleAsync(user, Roles.User);
```

هذا السطر مثل إعطاء العميل بطاقة فئة `User` بعد إنشاء حسابه.

### معالجة فشل التسجيل

`UserRegistrationException` تحمل قائمة الأخطاء من Identity، مثل تكرار الإيميل أو ضعف كلمة المرور.

`GlobalExceptionHandlingMiddleware` يحولها إلى `400 Bad Request` مع `ValidationErrorResponse` المنظم:

```json
{
  "statusCode": 400,
  "message": "User registration failed.",
  "errors": ["..."]
}
```

## 8. LoginAsync — تسجيل الدخول

المسار:

```text
POST /api/Auth/login
→ FindByEmailAsync
→ CheckPasswordAsync
→ CreateToken
→ UserDto
```

1. `FindByEmailAsync` يبحث عن المستخدم بالإيميل.
2. `CheckPasswordAsync` يقارن كلمة المرور المدخلة بالـ hash المخزن بأمان.
3. إذا لم يوجد المستخدم أو كلمة المرور خطأ، نرمي `InvalidCredentialsException`.
4. الـ Middleware يحولها إلى `400` برسالة آمنة.
5. إذا كانت صحيحة، نعيد `UserDto` وبداخله Token جديد.

عدم التفريق بين "المستخدم غير موجود" و"كلمة المرور خطأ" في الرسالة يساعد على عدم كشف معلومات للمهاجم.

## 9. JWT: البطاقة المؤقتة

الحزمة المستخدمة:

```text
Microsoft.AspNetCore.Authentication.JwtBearer
```

الـ JWT نص موقّع رقميًا، لا يحتاج السيرفر لحفظه في قاعدة البيانات مع كل طلب. يرسله العميل في Header:

```text
Authorization: Bearer <token>
```

إعداداته في `Menu.Web/appsettings.json`:

```json
"Jwt": {
  "Key": "...",
  "Issuer": "Menu.Web",
  "Audience": "Menu.Web.Client",
  "DurationInMinutes": 60
}
```

- `Key`: السر السري للتوقيع. في الإنتاج لا يوضع داخل Git أو `appsettings.json`.
- `Issuer`: الجهة التي أصدرت Token.
- `Audience`: التطبيق الذي سيتلقى Token.
- `DurationInMinutes`: مدة الصلاحية، وهي حاليًا 60 دقيقة.

`JwtOptions` داخل `Menu.Shared/Settings` هو كلاس منظم لقراءة هذه الإعدادات بدل كتابة النصوص في كل مكان.

## 10. Claims داخل Token

في `AuthService`، الدالة `CreateClaims` تجمع معلومات Token:

```csharp
new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
new Claim(ClaimTypes.Role, role)
```

الـ Claims مثل بيانات مطبوعة على بطاقة الدخول:

- الاسم.
- الإيميل.
- الدور أو الأدوار.

نستخدم:

```csharp
var roles = await _userManager.GetRolesAsync(user);
```

ثم نضيف كل Role إلى Token. لذلك Token المستخدم الجديد احتوى Claim باسم `role` وقيمتها `User`.

`CreateSigningCredentials` يوقّع الـ Token باستخدام `Key` وHMAC SHA-256؛ أي تعديل في محتوى Token يجعل التوقيع غير صالح.

## 11. التحقق من Token داخل Program.cs

تم تسجيل Authentication عبر `AddAuthentication().AddJwtBearer()` مع `TokenValidationParameters`.

السيرفر يفحص:

- هل `Issuer` صحيح؟
- هل `Audience` صحيح؟
- هل Token ما زال داخل مدة الصلاحية؟
- هل التوقيع صحيح باستخدام `Key`؟

وفي HTTP Pipeline:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

الترتيب مهم:

1. `UseAuthentication` يقرأ Token ويعرف المستخدم.
2. `UseAuthorization` يقرر هل لهذا المستخدم حق دخول الـ endpoint.

## 12. Role-Based Authorization على Basket

مكان ثوابت الأدوار الصحيح:

```text
Menu.Shared/Constants/Roles.cs
```

```csharp
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}
```

وضعناها في `Shared` لأنها معلومة مشتركة يستخدمها Presentation وPersistence. لا نكرر النص `"User"` في عدة ملفات، لأن الثابت الواحد يقلل أخطاء الكتابة.

في `BasketController`:

```csharp
[Authorize(Roles = Roles.User)]
```

هذا يعني: لا تدخل الـ Basket إلا إذا كان Token صالحًا ويحمل Role اسمها `User`.

## 13. 401 مقابل 403

| الحالة | المعنى | تشبيه |
|---|---|---|
| `401 Unauthorized` | لا يوجد Token صالح | الحارس لا يملك بطاقة تعريف لك. |
| `403 Forbidden` | Token صالح لكن Role غير مناسب | الحارس عرفك، لكن بطاقتك لا تمنحك دخول هذه الغرفة. |
| `200 OK` | Token صالح وRole `User` موجود | دخلت القسم المسموح لك به. |

تم توثيق `401` و`403` في مستوى `BasketController` عبر:

```csharp
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
```

هذه attributes توثق الاستجابات في Swagger، ولا تنفذ الحماية نفسها. التنفيذ الحقيقي يحدث بواسطة `[Authorize]` وMiddleware الخاص بـ Authentication/Authorization.

## 14. Swagger وBearer Token

أضفنا Bearer Definition وSecurity Requirement إلى Swagger. لذلك ظهر زر **Authorize**.

الاستعمال:

1. سجل دخول أو سجل حسابًا جديدًا.
2. انسخ `token` من الاستجابة.
3. اضغط **Authorize** في Swagger.
4. ألصق Token.
5. جرّب الـ endpoint المحمي.

Swagger يرسل بعدها Header `Authorization` تلقائيًا مع الطلبات المحمية.

## 15. الاختبارات التي نجحت

- تطبيق Migration الخاصة بـ Identity وإنشاء الجداول.
- `POST /api/Auth/register` مع مستخدم جديد: `200` ومعه Token.
- تسجيل مستخدم مكرر: `400` ورسائل Identity المنظمة.
- `POST /api/Auth/login` ببيانات صحيحة: `200` ومعه Token.
- Login خاطئ: `400`.
- Basket بدون Token: `401`.
- Basket بـ Token صحيح يحمل `User`: `200`.
- Basket بـ Token صحيح لحساب قديم بلا `User`: `403`.
- ظهور توثيق `401` و`403` في Swagger.

## 16. ملاحظات مهمة

- Token الذي أُنشئ قبل إضافة Role لا يحصل على Role تلقائيًا؛ أنشئ Token جديد بعد إسناد الدور.
- حسابات أُنشئت قبل سطر `AddToRoleAsync` ليس لها Role `User` تلقائيًا؛ لذلك ظهر معها `403`، وهذا اختبار صحيح.
- لا تشارك JWT Token في منشور أو GitHub؛ فهو تصريح دخول مؤقت.
- `Jwt:Key` الحالية مناسبة للتطوير فقط. في الإنتاج تستخدم Secret Manager أو Environment Variables.
- يمكن إضافة Role `Admin` مستقبلًا وحماية endpoints الإدارة بـ `[Authorize(Roles = Roles.Admin)]`.

## 17. أسئلة مراجعة ذاتية

1. لماذا نستخدم `ApplicationUser : IdentityUser` بدل كلاس مستخدم عادي؟
2. لماذا لا نعيد Password أو PasswordHash في `UserDto`؟
3. ما مسؤولية `UserManager`؟
4. لماذا نستدعي `AddToRoleAsync` بعد نجاح `CreateAsync`؟
5. ما الفرق بين Authentication وAuthorization؟
6. ما الفرق بين `401` و`403`؟
7. لماذا يجب أن يأتي `UseAuthentication` قبل `UseAuthorization`؟
8. ما هي Claims ولماذا نضع Role داخل Token؟
9. هل `ProducesResponseType` ينفذ الحماية أم يوثقها فقط؟
10. لماذا توجد `Roles` داخل `Menu.Shared`؟

## الخلاصة

Session 06 حولت الـ API من API مفتوحة إلى API تعرف المستخدم، تتحقق من هويته، وتتحكم في دخوله حسب دوره. المستخدم يسجل أو يسجل دخولًا، يحصل على JWT Token لمدة ساعة، ويرسلها مع الطلبات. الـ Basket لا تسمح إلا لمن يحمل Token صالحًا وفيه Role `User`.
