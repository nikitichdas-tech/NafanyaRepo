using Dapper;
using Microsoft.Data.SqlClient;
using System.Text;

namespace Nafanya.Services
{
    public class DatabaseService(string? connectionString, string[]? allowedIps)
    {
        private readonly string? _connectionString = connectionString;
        private readonly string[]? _allowedIps = allowedIps;

        // Проверка подключения к базе данных
        public static async Task TestLocalConnection()
        {
            try
            {
                var localConnectionString = "Server=.\\SQLEXPRESS;Database=ABCP_TestDB;Trusted_Connection=true;TrustServerCertificate=true;";

                using var connection = new SqlConnection(localConnectionString);
                await connection.OpenAsync();

                var result = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ServiceStatistics");
                Console.WriteLine($"Подключение к локальному серверу SQL прошло успешно! Записей найдено: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Подключение к локальному серверу SQL не удалось: {ex.Message}");
            }
        }
        // Данные для кнопки "Партнёры - запросы"
        public async Task<string> GetTopPartnersFromSql()

        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    return GetTestData();
                }

                using var connection = new SqlConnection(_connectionString + ";TrustServerCertificate=true;");
                await connection.OpenAsync();

                // SQL запрос к локальной базе данных по топ 5 клиентов
                var query = @"
SELECT TOP 5 
    CONCAT('Id=', PartnerId) AS PartnerId, 
    CAST(COUNT(*) AS decimal) / 3600.0 as RPS,
    AVG(CAST(ResponseTime AS decimal)) as AvgResponseTime,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
    COUNT(*) as TotalRequests
FROM ServiceStatistics 
WHERE Date >= DATEADD(HOUR, -1, GETDATE())
GROUP BY PartnerId 
ORDER BY COUNT(*) DESC";

                var parameters = new DynamicParameters();
                for (int i = 0; i < Math.Min(3, _allowedIps?.Length ?? 0); i++)
                {
                    parameters.Add($"@Ip{i + 1}", _allowedIps[i]);
                }

                var results = await connection.QueryAsync<(string PartnerId, decimal RPS, decimal AvgResponseTime, int SuccessCount, int TotalRequests)>(query, parameters);

                return FormatSqlResultsTop(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SQL Error: {ex.Message}");
                return GetTestData();
            }
        }

        private static string FormatSqlResultsTop(IEnumerable<(string PartnerId, decimal RPS, decimal AvgResponseTime, int SuccessCount, int TotalRequests)> results)
        {
            if (!results.Any())
                return "📊 *Данные не найдены за последний час*";

            var sb = new StringBuilder();
            sb.AppendLine("📊 *ТОП партнеров по запросам:*");
            sb.AppendLine("*(за последний час)*");
            sb.AppendLine();

            int position = 1;
            foreach (var result in results)
            {
                var successRate = result.TotalRequests > 0
                    ? (result.SuccessCount * 100.0m / result.TotalRequests)
                    : 0;

                sb.AppendLine($"#{position}. {result.PartnerId}");
                sb.AppendLine($"   • {result.RPS:F2} запр/сек");
                sb.AppendLine($"   • {result.AvgResponseTime:F0}мс среднее время");
                sb.AppendLine($"   • {successRate:F1}% успешных");
                sb.AppendLine();
                position++;
            }

            sb.AppendLine($"🕐 Обновлено: {DateTime.Now:HH:mm:ss}");
            return sb.ToString();
        }
        // Метод для тестовых данных
        private static string GetTestData()
        {
            return "📊 *ТОП партнеров по запросам в секунду:*\n\n" +
                   "#1. Id=12345: *1500.00 запр/сек*\n" +
                   "#2. Id=67890: *1200.50 запр/сек*\n" +
                   "#3. Id=54321: *900.75 запр/сек*\n\n" +
                   "🕐 Обновлено: " + DateTime.Now.ToString("HH:mm:ss") +
                   "\n\n⚠️ *Режим тестирования*";
        }
        // Данные для кнопки "Партнёры - заказы"
        public async Task<string> GetTopPartnersOrdersFromSql()

        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    return GetTestDataOrders();
                }

                using var connection = new SqlConnection(_connectionString + ";TrustServerCertificate=true;");
                await connection.OpenAsync();

                // SQL запрос к локальной базе данных
                var query = @"
        SELECT PartnerId, COUNT(*) as OrderCount 
        FROM ServiceStatistics 
        WHERE RequestType = 'ORDER' AND Date >= DATEADD(DAY, -1, GETDATE())
        GROUP BY PartnerId";

                var parameters = new DynamicParameters();
                for (int i = 0; i < Math.Min(3, _allowedIps?.Length ?? 0); i++)
                {
                    parameters.Add($"@Ip{i + 1}", _allowedIps[i]);
                }

                var results = await connection.QueryAsync<(string PartnerId, decimal OrderCount)>(query, parameters);

                return FormatSqlResultsOrders(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SQL Error: {ex.Message}");
                return GetTestDataOrders();
            }
        }

        private static string FormatSqlResultsOrders(IEnumerable<(string PartnerId, decimal OrderCount)> results)
        {
            if (!results.Any())
                return "📊 *Данные не найдены за последний час*";

            var sb = new StringBuilder();
            sb.AppendLine("📊 *Наибольшее количество заказов у партнёра:*");
            sb.AppendLine();

            int position = 1;
            foreach (var result in results)
            {
                sb.AppendLine($"#{position}. {result.PartnerId}");
                sb.AppendLine($"   • *{result.OrderCount:F2} заказов*");
                sb.AppendLine();
                position++;
            }

            sb.AppendLine($"🕐 Обновлено: {DateTime.Now:HH:mm:ss}");
            return sb.ToString();
        }
        // Метод для тестовых данных
        private static string GetTestDataOrders()
        {
            return "📊 *Наибольшее количество заказов у партнёра:*\n\n" +
                   "#1. Id=12345: *100500 заказов*\n" +
                   "🕐 Обновлено: " + DateTime.Now.ToString("HH:mm:ss") +
                   "\n\n⚠️ *Режим тестирования*";

        }

        public async Task<String> GetPartnerStatisticsFromSql(string partnerId)
        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    return GetTestPartnerStatistics(partnerId);
                }
                using var connection = new SqlConnection(_connectionString + ";TrustServerCertificate=true;");
                await connection.OpenAsync();

                // SQL запрос к локальной базе данных
                var query = @"
            SELECT 
                PartnerId,
                Ip,
                Date,
                RequestType,
                Status,
                ResponseTime
            FROM ServiceStatistics 
            WHERE PartnerId = @PartnerId 
                AND Date >= DATEADD(DAY, -7, GETDATE())
            ORDER BY Date DESC";
                var parameters = new { PartnerId = partnerId };

                var results = await connection.QueryAsync<(int PartnerId, string Ip, DateTime Date, string RequestType, string Status, int? ResponseTime)>(query, parameters);

                return FormatPartnerStatistics(results, partnerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка! SQL Error - невозможно получить статистику по партнёрам: {ex.Message}");
                return GetTestPartnerStatistics(partnerId);
            }
        }
        private string FormatPartnerStatistics(IEnumerable<(int PartnerId, string Ip, DateTime Date, string RequestType, string Status, int? ResponseTime)> results, string requestedPartnerId)
        {
            if (!results.Any())
            {
                return $"📊 *Статистика по PartnerID {requestedPartnerId}:*\n\n" +
                       "❌ Данные не найдены за последние 7 дней";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"🔍 *Статистика по PartnerID {requestedPartnerId}:*");
            sb.AppendLine("*(за последние 7 дней)*");
            sb.AppendLine();

            var firstRecord = results.First();
            sb.AppendLine($"🤝 *Партнер:* {firstRecord.PartnerId}");
            sb.AppendLine($"📅 *Период:* {results.Min(r => r.Date):dd.MM.yy} - {results.Max(r => r.Date):dd.MM.yy}");
            sb.AppendLine();

            // Общая статистика
            var totalRequests = results.Count();
            var successRequests = results.Count(r => r.Status == "Success");
            var successRate = totalRequests > 0 ? (successRequests * 100.0 / totalRequests) : 0;
            var avgResponseTime = results.Where(r => r.ResponseTime.HasValue).Average(r => r.ResponseTime);

            sb.AppendLine("📈 *Общая статистика:*");
            sb.AppendLine($"   • Всего запросов: *{totalRequests}*");
            sb.AppendLine($"   • Успешных: *{successRequests}* ({successRate:F1}%)");
            sb.AppendLine($"   • Среднее время: *{avgResponseTime:F0}мс*");
            sb.AppendLine();
            return sb.ToString();
        }
        private string GetTestPartnerStatistics(string partnerId)
        {
            return $"📊 *Статистика по PartnerID {partnerId}:*\n\n" +
                   "🤝 *Партнер:* 12345\n" +
                   "📅 *Период:* 01.12.23 - 08.12.23\n\n" +
                   "📈 *Общая статистика:*\n" +
                   "   • Всего запросов: *247*\n" +
                   "   • Успешных: *185* (74.9%)\n" +
                   "   • Среднее время: *142мс*\n\n" +
                   "🕐 Обновлено: " + DateTime.Now.ToString("HH:mm:ss") +
           "\n\n⚠️ *Режим тестирования*";
        }
    }
}
