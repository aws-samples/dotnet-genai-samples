using System.Diagnostics;

namespace Shop.Web.Services;

/// <summary>
/// Anonymous shopper session kept in a cookie. The id is also tagged on the
/// request span so you can find every trace for one shopper.
/// </summary>
public static class ShopSession
{
    private const string CookieName = "shop_session";
    private const string ItemKey = "ShopSessionId";

    public static IApplicationBuilder UseShopSession(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (!context.Request.Cookies.TryGetValue(CookieName, out var sessionId) || !Guid.TryParse(sessionId, out _))
            {
                sessionId = Guid.NewGuid().ToString("N");
                context.Response.Cookies.Append(CookieName, sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromDays(30),
                });
            }

            context.Items[ItemKey] = sessionId;
            Activity.Current?.SetTag("shop.session.id", sessionId);
            await next();
        });

    public static string GetShopSessionId(this HttpContext context) =>
        context.Items[ItemKey] as string ?? throw new InvalidOperationException("UseShopSession middleware is not registered.");
}
