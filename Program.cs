using AlanTelegramBotApp.Models;
using AlanTelegramBotApp.utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = AlanTelegramBotApp.Models.User;

namespace AlanTelegramBotApp;

internal class Program
{
    private static readonly ITelegramBotClient Bot =
        new TelegramBotClient("");

    private static readonly Dictionary<long, List<string>> userResponses = new();

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        var botEndMessage = await Api.GetTelegramMessagesAsync();

        Console.WriteLine(JsonConvert.SerializeObject(update));
        if (update.Type == UpdateType.Message)
        {
            var message = update.Message;
            var telegramId = message.From.Id;

            var chatId = message.Chat.Id;

            var isUserExistsInDataBase = await IsUserExistsInDataBase(telegramId);
            var isUserAdmin = await IsUserAdmin(telegramId);

            if (message != null && message.Text != null)
            {
                switch (message.Text.ToLower())
                {
                    case "/start":
                        userResponses[chatId] = new List<string>(); // Start a new conversation
                        await botClient.SendTextMessageAsync(message.Chat, "Введите любое слово для начала");
                        return;
                    case "/прием":
                        {
                            var appointment = await Api.GetAppointmentAsync(telegramId);

                            if (appointment == null)
                                await botClient.SendTextMessageAsync(message.Chat,
                                    "На данный момент приём для Вас не был назначен");
                            else
                                await botClient.SendTextMessageAsync(message.Chat,
                                    $"Дата и время вашего приёма - {appointment}");

                            userResponses.Remove(chatId);
                            return;
                        }
                    case "/таблица" when !isUserAdmin:
                        await botClient.SendTextMessageAsync(message.Chat, "Вы не админ");

                        userResponses.Remove(chatId);
                        return;
                    case "/таблица":
                        {
                            string patients = await Api.GetPatientsAsync();

                            if (String.IsNullOrEmpty(patients))
                            {
                                await botClient.SendTextMessageAsync(message.Chat,
                                    "На данный момент в БД нет запросов на запись");
                            }
                            else
                            {
                                var jsonData = JArray.Parse(patients);


                                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                                using (var package = new ExcelPackage())
                                {
                                    var worksheet = package.Workbook.Worksheets.Add("Patients");

                                    // Set headers
                                    worksheet.Cells[1, 1].Value = "Patient ID";
                                    worksheet.Cells[1, 2].Value = "Diagnosis";
                                    worksheet.Cells[1, 3].Value = "Full Name";
                                    worksheet.Cells[1, 4].Value = "Birth Date";
                                    worksheet.Cells[1, 5].Value = "User's Full Name";
                                    worksheet.Cells[1, 6].Value = "Telephone Number";
                                    worksheet.Cells[1, 7].Value = "City";
                                    worksheet.Cells[1, 8].Value = "Telegram ID";

                                    var row = 2;

                                    foreach (var item in jsonData)
                                    {
                                        worksheet.Cells[row, 1].Value = item["patient"]["patientId"].ToString();
                                        worksheet.Cells[row, 2].Value = item["patient"]["diagnosis"].ToString();
                                        worksheet.Cells[row, 3].Value = item["patient"]["fullName"].ToString();
                                        worksheet.Cells[row, 4].Value = item["patient"]["birthDate"].ToString();
                                        worksheet.Cells[row, 5].Value = item["user"]["fullName"].ToString();
                                        worksheet.Cells[row, 6].Value = item["user"]["telephoneNumber"].ToString();
                                        worksheet.Cells[row, 7].Value = item["user"]["city"].ToString();
                                        worksheet.Cells[row, 8].Value = item["patient"]["telegramId"].ToString();

                                        row++;
                                    }

                                    // Save the Excel package to a stream
                                    using (var stream = new MemoryStream())
                                    {
                                        package.SaveAs(stream);
                                        stream.Position = 0;

                                        // Send the Excel file to the user
                                        var iof = new InputFileStream(stream);
                                        await botClient.SendDocumentAsync(message.Chat.Id, iof);
                                    }
                                }
                            }

                            userResponses.Remove(chatId);
                            return;
                        }
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat,
                    "Вы не можете отправлять видео или фото! Пожалуйста, используйте лишь текст! Напишите /start ещё раз для продолжения записи!");
                userResponses.Remove(chatId);
                return;
            }

            if (!userResponses.ContainsKey(chatId))
            {
                await botClient.SendTextMessageAsync(message.Chat,
                    "Напишите /start или /прием для записи или проверки приема соотвественно.");
                return;
            }

            userResponses[chatId].Add(message.Text);

            switch (userResponses[chatId].Count)
            {
                case 1:
                    await botClient.SendTextMessageAsync(message.Chat, "Введите ФИО родителя");
                    break;
                case 2:
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Введите контактный номер телефона родителя в формате +7");
                    break;
                case 3:
                    await botClient.SendTextMessageAsync(message.Chat, "Введите город вашего проживания");
                    break;
                case 4:
                    await botClient.SendTextMessageAsync(message.Chat, "Введите диагноз ребенка");
                    break;
                case 5:
                    await botClient.SendTextMessageAsync(message.Chat, "Введите ФИО ребенка");
                    break;
                case 6:
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Введите дату рождения ребенка в формате ДЕНЬ/МЕСЯЦ/ГОД. Пример - 1/8/1999");
                    break;
                case 7:
                    await botClient.SendTextMessageAsync(message.Chat, botEndMessage);
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Удостоверьтесь в корректности данных! Если это ваши данные введите ДА! Если же данные неверны или вы хотите отменить запись введите НЕТ");
                    break;
                case 8 when message.Text.ToLower() == "да":
                {
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Спасибо за обращение! Проверить попадание на то, попали ли вы на приём можно по команду /прием");

                    var currentUser = CreateUserModel(userResponses, telegramId, isUserAdmin);

                    if (isUserExistsInDataBase)
                    {
                        await UpdateUser(currentUser);
                        await CreatePatient(userResponses, telegramId);
                    }
                    else
                    {
                        await CreateUser(currentUser);
                        await CreatePatient(userResponses, telegramId);
                        isUserExistsInDataBase = true;
                    }

                    userResponses.Remove(chatId);
                    return;
                }
                case 8 when message.Text.ToLower() == "нет":
                    await botClient.SendTextMessageAsync(message.Chat, "Вы отказались от записи.");
                    userResponses.Remove(chatId);
                    return;
                case 8:
                    await botClient.SendTextMessageAsync(message.Chat,
                        "Непонятный ответ. Пожалуйста, введите ДА или НЕТ.");
                    return;
            }
        }
    }

    private static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(JsonConvert.SerializeObject(exception));
    }

    private static async Task<string> GetUser(long telegramId)
    {
        var user = await Api.GetUserAsync(telegramId);

        if (string.IsNullOrEmpty(user)) return null;

        return user;
    }

    private static async Task<bool> IsUserExistsInDataBase(long telegramId)
    {
        var user = await GetUser(telegramId);

        if (string.IsNullOrEmpty(user)) return false;

        return true;
    }

    private static async Task<bool> IsUserAdmin(long telegramId)
    {
        var user = await Api.GetUserAsync(telegramId);

        if (string.IsNullOrEmpty(user)) return false;

        var userJ = JObject.Parse(user);
        var isAdmin = userJ["isAdmin"];

        if (isAdmin != null && isAdmin.Type == JTokenType.Boolean)
        {
            var isAdminBool = (bool)isAdmin;

            return isAdminBool ? true : false;
        }

        return false;
    }

    private static User CreateUserModel(Dictionary<long, List<string>> userResponses, long telegramId, bool isAdmin)
    {
        var responses = userResponses.ContainsKey(telegramId) ? userResponses[telegramId] : new List<string>();

        var newUser = new User
        {
            TelegramId = telegramId,
            FullName = responses.Count > 1 ? responses[1] : null,
            TelephoneNumber = responses.Count > 2 ? responses[2] : null,
            City = responses.Count > 3 ? responses[3] : null,
            IsAdmin = isAdmin
        };

        return newUser;
    }

    private static async Task CreateUser(User user)
    {
        await Api.CreateUserAsync(user);
    }

    private static async Task UpdateUser(User user)
    {
        await Api.UpdateUserAsync(user);
    }

    private static async Task CreatePatient(Dictionary<long, List<string>> userResponses, long telegramId)
    {
        var responses = userResponses.ContainsKey(telegramId) ? userResponses[telegramId] : new List<string>();
        var nextAvailablePatientId = await Api.GetNextAvailablePatientIdAsync();

        var birthDateStr = responses.Count > 6 ? responses[6] : null;

        var newPatient = new Patient
        {
            PatientId = nextAvailablePatientId,
            Diagnosis = responses.Count > 4 ? responses[4] : null,
            FullName = responses.Count > 5 ? responses[5] : null,
            BirthDate = birthDateStr,
            TelegramId = telegramId
        };

        await Api.CreatePatientAsync(newPatient);
    }

    private static void Main(string[] args)
    {
        Console.WriteLine("Запущен бот " + Bot.GetMeAsync().Result.FirstName);

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions();
        Bot.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );
        Console.ReadLine();
    }
}