namespace LibraryPro.Web.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        public string GenerateOverdueNoticeEmail(string memberName, string bookTitle, DateTime dueDate, decimal fineAmount)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Overdue Book Notice</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .button {{ display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 5px; }}
        .alert {{ background: #f8d7da; color: #721c24; padding: 15px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📚 LibraryPro - Overdue Notice</h1>
        </div>
        <div class='content'>
            <p>Dear {memberName},</p>
            <div class='alert'>
                <strong>Attention:</strong> You have an overdue book that needs to be returned immediately.
            </div>
            <h3>Book Details:</h3>
            <ul>
                <li><strong>Title:</strong> {bookTitle}</li>
                <li><strong>Due Date:</strong> {dueDate:MMMM dd, yyyy}</li>
                <li><strong>Current Fine:</strong> ₹{fineAmount:F2}</li>
            </ul>
            <p>Please return the book as soon as possible to avoid additional fines. The fine increases by ₹10 per day.</p>
            <p>If you have already returned this book, please contact the library staff.</p>
            <p style='margin-top: 20px;'>
                <a href='http://localhost:5002/Members/Profile' class='button'>View Your Account</a>
            </p>
        </div>
        <div class='footer'>
            <p>This is an automated message from LibraryPro. Please do not reply to this email.</p>
            <p>© {DateTime.Now.Year} LibraryPro. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateDueDateReminderEmail(string memberName, string bookTitle, DateTime dueDate, int daysUntilDue)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Due Date Reminder</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #28a745; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .button {{ display: inline-block; padding: 10px 20px; background: #28a745; color: white; text-decoration: none; border-radius: 5px; }}
        .info {{ background: #d1ecf1; color: #0c5460; padding: 15px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📚 LibraryPro - Due Date Reminder</h1>
        </div>
        <div class='content'>
            <p>Dear {memberName},</p>
            <div class='info'>
                <strong>Reminder:</strong> Your book is due in {daysUntilDue} day{(daysUntilDue > 1 ? "s" : "")}.
            </div>
            <h3>Book Details:</h3>
            <ul>
                <li><strong>Title:</strong> {bookTitle}</li>
                <li><strong>Due Date:</strong> {dueDate:MMMM dd, yyyy}</li>
            </ul>
            <p>Please return or renew the book before the due date to avoid late fees.</p>
            <p style='margin-top: 20px;'>
                <a href='http://localhost:5002/Members/Profile' class='button'>View Your Account</a>
            </p>
        </div>
        <div class='footer'>
            <p>This is an automated message from LibraryPro. Please do not reply to this email.</p>
            <p>© {DateTime.Now.Year} LibraryPro. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateWelcomeEmail(string memberName, string memberEmail)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to LibraryPro</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #6c757d; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .button {{ display: inline-block; padding: 10px 20px; background: #6c757d; color: white; text-decoration: none; border-radius: 5px; }}
        .success {{ background: #d4edda; color: #155724; padding: 15px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📚 Welcome to LibraryPro!</h1>
        </div>
        <div class='content'>
            <div class='success'>
                <strong>Welcome, {memberName}!</strong>
            </div>
            <p>Thank you for joining LibraryPro. Your account has been successfully created.</p>
            <h3>Your Account Details:</h3>
            <ul>
                <li><strong>Name:</strong> {memberName}</li>
                <li><strong>Email:</strong> {memberEmail}</li>
            </ul>
            <p>You can now browse our catalog, borrow books, and manage your account online.</p>
            <p style='margin-top: 20px;'>
                <a href='http://localhost:5002/Books/Index' class='button'>Browse Catalog</a>
            </p>
        </div>
        <div class='footer'>
            <p>This is an automated message from LibraryPro. Please do not reply to this email.</p>
            <p>© {DateTime.Now.Year} LibraryPro. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public string GenerateReservationAvailableEmail(string memberName, string bookTitle)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reservation Available</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #17a2b8; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background: #f9f9f9; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .button {{ display: inline-block; padding: 10px 20px; background: #17a2b8; color: white; text-decoration: none; border-radius: 5px; }}
        .success {{ background: #d1ecf1; color: #0c5460; padding: 15px; border-radius: 5px; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📚 LibraryPro - Reservation Available</h1>
        </div>
        <div class='content'>
            <p>Dear {memberName},</p>
            <div class='success'>
                <strong>Good News!</strong> Your reserved book is now available for pickup.
            </div>
            <h3>Book Details:</h3>
            <ul>
                <li><strong>Title:</strong> {bookTitle}</li>
            </ul>
            <p>Please visit the library to collect your book within 3 days. After this period, the reservation will be cancelled.</p>
            <p style='margin-top: 20px;'>
                <a href='http://localhost:5002/Members/Profile' class='button'>View Your Account</a>
            </p>
        </div>
        <div class='footer'>
            <p>This is an automated message from LibraryPro. Please do not reply to this email.</p>
            <p>© {DateTime.Now.Year} LibraryPro. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
