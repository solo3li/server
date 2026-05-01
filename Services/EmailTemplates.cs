namespace Uis.Server.Services;

public static class EmailTemplates
{
    private const string PrimaryColor = "#6366F1";
    private const string SecondaryColor = "#A855F7";
    private const string FontStack = "'Tajawal', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif";

    public static string GetDefaultBaseTemplate()
    {
        return $@"
<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <link href='https://fonts.googleapis.com/css2?family=Tajawal:wght@400;500;700;800;900&display=swap' rel='stylesheet'>
    <style>
        body {{ margin: 0; padding: 0; background-color: #f1f5f9; -webkit-font-smoothing: antialiased; }}
        .container {{ max-width: 600px; margin: 40px auto; background-color: #ffffff; border-radius: 32px; overflow: hidden; box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.08); }}
        .header {{ background: linear-gradient(135deg, {PrimaryColor} 0%, {SecondaryColor} 100%); padding: 60px 40px; text-align: center; color: #ffffff; }}
        .content {{ padding: 50px 40px; text-align: center; }}
        .footer {{ background-color: #f8fafc; padding: 30px 40px; text-align: center; border-top: 1px solid #e2e8f0; }}
        h1 {{ margin: 0; font-size: 42px; font-weight: 900; letter-spacing: 4px; text-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        h2 {{ color: #1e293b; font-size: 26px; font-weight: 800; margin-bottom: 20px; }}
        p {{ color: #64748b; font-size: 16px; line-height: 1.8; margin: 0; }}
        .badge {{ display: inline-block; padding: 6px 16px; background-color: rgba(255,255,255,0.2); border-radius: 100px; font-size: 13px; font-weight: 700; margin-top: 15px; }}
    </style>
</head>
<body style='font-family: {FontStack};'>
    <div class='container'>
        <div class='header'>
            <h1>UIS</h1>
            <div class='badge'>المنصة الجامعية الذكية</div>
        </div>
        <div class='content'>
            <h2>{{TITLE}}</h2>
            <div style='color: #475569; font-size: 16px;'>{{CONTENT}}</div>
        </div>
        <div class='footer'>
            <p style='font-size: 13px; color: #94a3b8;'>هذا بريد تلقائي، يرجى عدم الرد عليه مباشرة.</p>
            <div style='margin-top: 20px;'>
                <a href='#' style='color: {PrimaryColor}; text-decoration: none; font-weight: 700; font-size: 12px; margin: 0 10px;'>الدعم الفني</a>
                <span style='color: #cbd5e1;'>•</span>
                <a href='#' style='color: {PrimaryColor}; text-decoration: none; font-weight: 700; font-size: 12px; margin: 0 10px;'>خصوصية البيانات</a>
            </div>
            <p style='font-size: 11px; color: #cbd5e1; margin-top: 25px;'>© 2026 UIS Platform. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }

    public static string Wrap(string title, string content, string? buttonText = null, string? buttonUrl = null, string? baseTemplate = null)
    {
        var html = baseTemplate ?? GetDefaultBaseTemplate();

        var finalContent = content;
        if (!string.IsNullOrEmpty(buttonText) && !string.IsNullOrEmpty(buttonUrl))
        {
            finalContent += $@"
            <div style='margin-top: 40px;'>
                <a href='{buttonUrl}' style='background: linear-gradient(135deg, {PrimaryColor} 0%, {SecondaryColor} 100%); color: #ffffff; padding: 16px 36px; border-radius: 16px; text-decoration: none; font-weight: 800; font-size: 16px; display: inline-block; box-shadow: 0 10px 20px -5px rgba(99, 102, 241, 0.4); transition: all 0.3s ease;'>
                    {buttonText}
                </a>
            </div>";
        }

        return html.Replace("{TITLE}", title).Replace("{CONTENT}", finalContent);
    }

    public static string GetOtpTemplate(string code)
    {
        var content = $@"
        <p>مرحباً بك في UIS! لإتمام عملية الدخول، يرجى استخدام رمز التحقق التالي:</p>
        <div style='margin: 35px 0; background-color: #f8fafc; border: 2px dashed {PrimaryColor}; border-radius: 24px; padding: 30px; display: inline-block;'>
            <span style='font-size: 48px; font-weight: 900; color: {PrimaryColor}; letter-spacing: 15px; font-family: monospace;'>{code}</span>
        </div>
        <p style='font-size: 14px; font-weight: 500;'>الرمز صالح لمدة 10 دقائق فقط. لا تشارك هذا الرمز مع أي شخص.</p>";
        
        return Wrap("تأكيد هويتك", content);
    }

    public static string GetWelcomeTemplate(string name)
    {
        var content = $@"
        <p>أهلاً بك يا <strong>{name}</strong> في عائلة UIS!</p>
        <p>نحن سعداء جداً بانضمامك إلينا. الآن يمكنك البدء في طلب الخدمات الجامعية أو العمل كمنفذ للمشاريع.</p>
        <p style='margin-top: 15px;'>اكتشف عالمنا الجديد وجرب خدماتنا المتميزة.</p>";
        
        return Wrap("مرحباً بك في رحاب UIS", content, "ابدأ الآن", "https://uis-app.com/get-started");
    }

    public static string GetNotificationTemplate(string title, string message)
    {
        var content = $@"<p>{message}</p>";
        return Wrap(title, content, "عرض التفاصيل", "https://uis-app.com/notifications");
    }
}
