using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ForgeTales.Classes;
using ForgeTales.Model;
using ForgeTales.Pages;
using Npgsql;

namespace ForgeTales.Windows
{
    public partial class MainMenu : Window
    {
        // Локальная база знаний чат-бота (C# 7.3 совместимая)
        private readonly Dictionary<string[], string> _knowledgeBase = new Dictionary<string[], string>()
        {
            // Навигация
            { new[] { "навигация", "разделы", "меню" }, "Основные разделы:\n\n• Все новеллы - главная страница с каталогом\n• Мои новеллы - для авторов (публикация/редактирование)\n• Профиль - настройки аккаунта\n• Подписки - доступ к платному контенту\n• Новости AniVisual - наш партнерский проект" },
            { new[] { "все новеллы", "главная" }, "На главной странице вы найдете:\n\n• Новинки платформы\n• Топ новелл по рейтингу\n• Фильтры по жанрам\n• Поиск по названию/автору\n• Рекомендации под ваш вкус" },
            { new[] { "мои новеллы", "авторский раздел" }, "Функционал для авторов:\n\n• Создание новых новелл\n• Редактирование существующих\n• Управление главами\n• Просмотр статистики\n• Общение с читателями" },
            { new[] { "профиль", "аккаунт" }, "В профиле можно:\n\n• Изменить имя/аватар\n• Обновить email\n• Сменить пароль\n• Посмотреть историю чтения\n• Управлять уведомлениями" },
            { new[] { "подписки", "премиум" }, "Преимущества подписки:\n\n• Мгновенный доступ ко всем главам\n• Без рекламы\n• Эксклюзивные новеллы\n• 500 сердечек в месяц\n• Цена: 299₽/месяц" },
            { new[] { "сердечки", "валюта" }, "Сердечки - это:\n\n• Внутренняя валюта (1₽ = 1 сердечко)\n• Тратится на доступ к главам\n• Без подписки: 1 глава = 10 сердечек\n• С подпиской: неограниченный доступ" },
            { new[] { "новости" }, "В графе с новостями у нас собраны: \n\n• Новостной портал об онгоингах аниме и манги\n• Форум для обсуждений" },

            // Визуальные новеллы
            { new[] { "визуальные новеллы", "что это" }, "Визуальные новеллы - это:\n\n• Интерактивные текстовые игры\n• Ветвящиеся сюжеты\n• Иллюстрации и музыка\n• Диалоги с выбором действий\n• Примеры: Doki Doki, Fate/stay night" },
            { new[] { "создать новеллу", "движок" }, "Для создания новелл рекомендуем:\n\n• Ren'Py - бесплатный движок\n• Поддержка Python-скриптов\n• Кроссплатформенность\n• Подробные туториалы на нашем форуме" },
            { new[] { "ренпай", "renpy" }, "Ren'Py - это:\n\n• Самый популярный движок для VN\n• Простой язык сценариев\n• Поддержка анимаций\n• Экспорт в Windows/Mac/Android/iOS\n• Официальный сайт: renpy.org" },

            // О проекте
            { new[] { "forgetales", "о проекте" }, "ForgeTales - это:\n\n• Платформа для чтения/создания VN\n• Сообщество авторов и читателей\n• Монетизация для создателей\n• Запущен в 2025 году\n• 50 000+ пользователей" },
            { new[] { "команда", "разработчики" }, "Наша команда:\n\n• Снежа - CEO\n• Снежа - дизайн\n• Снежа - разработка\n• Снежа - модерация\n• Артур - моральная поддержка\n• Контакты: support@forgetales.ru" },

            // Социальное взаимодействие
            { new[] { "как дела", "как ты" }, "Все отлично! Готов помочь с вопросами о платформе или просто поболтать :)" },
            { new[] { "спасибо", "благодарю" }, "Всегда пожалуйста! Обращайтесь, если понадобится помощь." },
            { new[] { "пока", "выход" }, "До встречи! Заглядывайте в любое время." },
            { new[] { "шутка", "анекдот" }, "Почему программист пишет новеллы?\nПотому что else для него - это не просто условие, а целая ветка сюжета!" },
            
            // Техподдержка
            { new[] { "ошибка", "баг" }, "Если нашли ошибку:\n\n1. Сделайте скриншот\n2. Опишите шаги воспроизведения\n3. Отправьте на support@forgetales.ru\nМы исправим это в ближайшем обновлении!" },
            { new[] { "контакты", "поддержка" }, "Связь с нами:\n\n• Email: support@forgetales.ru\n• Телеграм: @forgetales_support" }
        };

        // Альтернативные варианты ответов
        private readonly Dictionary<string, List<string>> _alternativeResponses = new Dictionary<string, List<string>>()
        {
            { "привет", new List<string>{ "Приветствую в ForgeTales!", "Здравствуйте! Чем займемся сегодня?", "Привет! Готов помочь с новеллами." } },
            { "пока", new List<string>{ "До скорой встречи!", "Возвращайтесь скорее!", "Хорошего дня!" } }
        };

        // Состояние приложения
        public MainMenuViewModel ViewModel { get; set; }
        private Reader _currentReader;
        private Author _currentAuthor;
        private bool _isChatMinimized = false;

        public MainMenu()
        {
            InitializeComponent();
            ViewModel = new MainMenuViewModel();
            DataContext = ViewModel;
            LoadCurrentUser();
            LogAction("app_launch");
        }

        #region Инициализация пользователя
        private void LoadCurrentUser()
        {
            if (Application.Current.Properties["CurrentUser"] is Reader reader)
            {
                _currentReader = reader;
                ViewModel.CurrentUser = reader;
                ViewModel.Username = reader.Username;
                ViewModel.Email = reader.Email;
                LoadAvatar();
                ShowNovelsPage(); // Перемещаем сюда
            }
            else if (Application.Current.Properties["CurrentUser"] is Author author)
            {
                _currentAuthor = author;
                ViewModel.CurrentUser = author;
                ViewModel.Username = author.Name;
                ViewModel.Email = author.Email;
                LoadAvatar();
                ShowNovelsPage(); // Перемещаем сюда
            }
            else
            {
                MainWindow.Logout();
                Close();
            }
        }

        public void UpdateAvatar(string newAvatarUrl)
        {
            if (_currentReader != null)
                _currentReader.AvatarUrl = newAvatarUrl;
            else if (_currentAuthor != null)
                _currentAuthor.AvatarUrl = newAvatarUrl;

            LoadAvatar(); // Перезагружаем аватар
        }

        private void LoadAvatar()
        {
            string avatarUrl = _currentReader?.AvatarUrl ?? _currentAuthor?.AvatarUrl;

            if (!string.IsNullOrEmpty(avatarUrl))
            {
                if (IsBase64String(avatarUrl))
                {
                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(avatarUrl);
                        using (var ms = new MemoryStream(imageBytes))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            AvatarImageBrush.ImageSource = bitmap;
                        }
                    }
                    catch
                    {
                        SetDefaultAvatar();
                    }
                }
                else if (File.Exists(avatarUrl))
                {
                    AvatarImageBrush.ImageSource = new BitmapImage(new Uri(avatarUrl));
                }
                else
                {
                    SetDefaultAvatar();
                }
            }
            else
            {
                SetDefaultAvatar();
            }
        }
        private void SetDefaultAvatar()
        {
            AvatarImageBrush.ImageSource = new BitmapImage(
                new Uri("pack://application:,,,/Images/default-avatar.png"));
        }

        private bool IsBase64String(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;

            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion


        #region Навигация
        private void ShowNovelsPage()
        {
            if (_currentReader != null)
            {
                MainFrame.Navigate(new NovelsPage(_currentReader));
            }
            else if (_currentAuthor != null)
            {
                MainFrame.Navigate(new NovelsPage(_currentAuthor));
            }
            else
            {
                // Удаляем вызов конструктора без параметров
                MainWindow.Logout();
                Close();
            }
        }

        private void NavigateToNovels(object sender, RoutedEventArgs e)
        {
            ShowNovelsPage();
            LogAction("novels_view");
        }

        private void NavigateToProfile(object sender, RoutedEventArgs e)
        {
            if (_currentReader != null)
            {
                MainFrame.Navigate(new EditProfilePage(_currentReader));
            }
            else if (_currentAuthor != null)
            {
                MainFrame.Navigate(new EditProfilePage(_currentAuthor));
            }
            LogAction("profile_view");
        }

        private void NavigateToMyNovels(object sender, RoutedEventArgs e)
        {
            if (_currentAuthor != null)
            {
                MainFrame.Navigate(new AuthorNovelsPage(_currentAuthor));
            }
            else if (_currentReader != null)
            {
                var authorInfo = new AuthorFromDb().GetAuthorByReaderId(_currentReader.ReaderId);
                if (authorInfo != null)
                {
                    MainFrame.Navigate(new AuthorNovelsPage(authorInfo));
                }
            }
            LogAction("my_novels_view");
        }

        private void NavigateToSubscriptions(object sender, RoutedEventArgs e)
        {
            if (_currentReader != null)
            {
                MainFrame.Navigate(new SubscriptionPage(_currentReader));
                LogAction("subscriptions_view");
            }
        }

        private void NavigateToAnivisualNews(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AniVisualNewsPage());
            LogAction("anivisual_news_view");
        }

        private void MyNovelsButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (_currentAuthor != null)
            {
                NavigateButton.Visibility = Visibility.Visible;
            }
            else if (_currentReader != null)
            {
                var authorInfo = new AuthorFromDb().GetAuthorByReaderId(_currentReader.ReaderId);
                NavigateButton.Visibility = authorInfo != null ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                NavigateButton.Visibility = Visibility.Collapsed;
            }
        }
        #endregion

        #region Чат-бот (обновленный с доступом к БД)
        private void InitializeChat()
        {
            ChatMessages.Children.Clear();
            AddChatMessage("Новик", "Добро пожаловать в ForgeTales! Я ваш помощник Новик. Можете спросить о:\n\n• Навигации по платформе\n• Создании новелл\n• Подписках и оплате\n• Технических вопросах\n• Жанрах и авторах\n• Конкретных новеллах\nИли просто поболтать :)", false);
        }

        private string GetResponse(string userMessage)
        {
            string lowerMsg = userMessage.ToLower();

            // Альтернативные ответы
            foreach (var pair in _alternativeResponses)
            {
                if (pair.Key.Split(',').Any(k => lowerMsg.Contains(k.Trim())))
                {
                    return pair.Value[new Random().Next(pair.Value.Count)];
                }
            }

            // Проверка запросов к БД
            if (lowerMsg.Contains("жанр") || lowerMsg.Contains("категори") || lowerMsg.Contains("стиль"))
            {
                return GetGenresResponse();
            }
            else if (lowerMsg.Contains("автор") || lowerMsg.Contains("писател") || lowerMsg.Contains("создател"))
            {
                return GetAuthorsResponse(lowerMsg);
            }
            else if (lowerMsg.Contains("новелл") || lowerMsg.Contains("истори") || lowerMsg.Contains("книг") || lowerMsg.Contains("произведен"))
            {
                return GetNovelsResponse(lowerMsg);
            }

            // Основная база знаний
            foreach (var entry in _knowledgeBase)
            {
                if (entry.Key.Any(keyword => lowerMsg.Contains(keyword)))
                {
                    return entry.Value;
                }
            }

            // Умные ответы на непонятные вопросы
            if (lowerMsg.Contains("?"))
            {
                return new List<string>
        {
            "Интересный вопрос! Но я лучше разбираюсь в функциях ForgeTales.",
            "Уточните, пожалуйста, это вопрос о платформе или о конкретной новелле?",
            "Попробуйте задать вопрос по-другому или выберите тему из меню.",
            "К сожалению, я не могу ответить на этот вопрос. Обратитесь в поддержку."
        }[new Random().Next(4)];
            }

            // Общие ответы
            return new List<string>
    {
        "Извините, я не совсем понял. Можете переформулировать?",
        "Попробуйте спросить о новеллах, профиле или подписках.",
        "Я пока учусь! Задайте вопрос проще или выберите из меню.",
        "О! Это напоминает мне сюжет одной новеллы... Но лучше спросите о платформе.",
        "Чтобы я лучше понял, уточните ваш вопрос о ForgeTales."
    }[new Random().Next(5)];
        }

        private string GetGenresResponse()
        {
            try
            {
                using (var conn = new NpgsqlConnection(DbConnection.connectionStr))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT name FROM genres ORDER BY name", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var genres = new List<string>();
                            while (reader.Read())
                            {
                                genres.Add(reader.GetString(0));
                            }

                            if (genres.Count > 0)
                            {
                                return "Доступные жанры на платформе:\n\n• " + string.Join("\n• ", genres) +
                                       "\n\nВы можете искать новеллы по конкретному жанру через поиск!";
                            }
                            return "К сожалению, информация о жанрах временно недоступна.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении жанров: {ex.Message}");
                return "Произошла ошибка при загрузке списка жанров. Попробуйте позже.";
            }
        }

        private string GetAuthorsResponse(string lowerMsg)
        {
            try
            {
                string searchTerm = "";
                if (lowerMsg.Contains("топ") || lowerMsg.Contains("популярн"))
                {
                    searchTerm = "ORDER BY (SELECT COUNT(*) FROM novels WHERE author_id = authors.author_id) DESC LIMIT 5";
                }
                else if (lowerMsg.Contains("новые") || lowerMsg.Contains("недавние"))
                {
                    searchTerm = "ORDER BY created_at DESC LIMIT 5";
                }

                using (var conn = new NpgsqlConnection(DbConnection.connectionStr))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        $"SELECT name, bio FROM authors {searchTerm}", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var authors = new List<string>();
                            while (reader.Read())
                            {
                                string bio = reader.IsDBNull(1) ? "" : $"\n   {reader.GetString(1).Substring(0, Math.Min(50, reader.GetString(1).Length))}...";
                                authors.Add($"{reader.GetString(0)}{bio}");
                            }

                            if (authors.Count > 0)
                            {
                                string header = lowerMsg.Contains("топ") ? "Топ-5 популярных авторов:" :
                                              lowerMsg.Contains("новые") ? "Новые авторы на платформе:" :
                                              "Авторы на платформе:";

                                return $"{header}\n\n• " + string.Join("\n• ", authors) +
                                       "\n\nВы можете найти все их работы через поиск!";
                            }
                            return "К сожалению, информация об авторах временно недоступна.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении авторов: {ex.Message}");
                return "Произошла ошибка при загрузке списка авторов. Попробуйте позже.";
            }
        }

        private string GetNovelsResponse(string lowerMsg)
        {
            try
            {
                string whereClause = "";
                if (lowerMsg.Contains("новые") || lowerMsg.Contains("недавние"))
                {
                    whereClause = "ORDER BY n.created_at DESC LIMIT 5";
                }
                else if (lowerMsg.Contains("популярн") || lowerMsg.Contains("топ"))
                {
                    whereClause = "ORDER BY (SELECT COUNT(*) FROM reading_sessions WHERE novel_id = n.novel_id) DESC LIMIT 5";
                }
                else if (lowerMsg.Contains("роман") || lowerMsg.Contains("романти"))
                {
                    whereClause = "WHERE g.name LIKE '%Романтика%' ORDER BY n.created_at DESC LIMIT 5";
                }
                else if (lowerMsg.Contains("фантаст") || lowerMsg.Contains("фэнтези"))
                {
                    whereClause = "WHERE g.name LIKE '%Фантастика%' OR g.name LIKE '%Фэнтези%' ORDER BY n.created_at DESC LIMIT 5";
                }

                using (var conn = new NpgsqlConnection(DbConnection.connectionStr))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        $"SELECT n.title, a.name, g.name FROM novels n " +
                        "JOIN authors a ON n.author_id = a.author_id " +
                        "LEFT JOIN genres g ON n.genre = g.genre_id " +
                        $"{whereClause}", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            var novels = new List<string>();
                            while (reader.Read())
                            {
                                string genre = reader.IsDBNull(2) ? "" : $" ({reader.GetString(2)})";
                                novels.Add($"{reader.GetString(0)} - {reader.GetString(1)}{genre}");
                            }

                            if (novels.Count > 0)
                            {
                                string header = lowerMsg.Contains("новые") ? "Новые новеллы на платформе:" :
                                              lowerMsg.Contains("популярн") ? "Популярные новеллы:" :
                                              lowerMsg.Contains("роман") ? "Романтические новеллы:" :
                                              lowerMsg.Contains("фантаст") ? "Фантастика и фэнтези:" :
                                              "Новеллы на платформе:";

                                return $"{header}\n\n• " + string.Join("\n• ", novels) +
                                       "\n\nВы можете найти их через поиск по названию!";
                            }
                            return "К сожалению, информация о новеллах временно недоступна.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при получении новелл: {ex.Message}");
                return "Произошла ошибка при загрузке списка новелл. Попробуйте позже.";
            }
        }

        private async Task ProcessUserMessage()
        {
            if (string.IsNullOrWhiteSpace(ChatInput.Text))
                return;

            string userMessage = ChatInput.Text.Trim();
            AddChatMessage("Вы", userMessage, true);
            ChatInput.Text = "";

            // Имитация задержки "печатания" с анимацией
            var typing = AddTypingIndicator();
            await Task.Delay(new Random().Next(800, 1500)); // Случайная задержка
            ChatMessages.Children.Remove(typing);

            string response = GetResponse(userMessage);
            AddChatMessage("Новик", response, false);
        }

        private void AddChatMessage(string sender, string message, bool isUser)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = isUser ? new SolidColorBrush(Color.FromRgb(220, 240, 255)) :
                                     new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBlock = new TextBlock
            {
                Text = $"{sender}: {message}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = isUser ? Brushes.DarkBlue : Brushes.Black,
                FontFamily = new FontFamily("Segoe UI")
            };

            border.Child = textBlock;
            ChatMessages.Children.Add(border);
            ChatHistory.ScrollToEnd();
        }

        private UIElement AddTypingIndicator()
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

            var dots = new TextBlock
            {
                Text = "Новик печатает",
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var dotAnimation = new TextBlock();
            var storyboard = new System.Windows.Media.Animation.Storyboard();
            var animation = new System.Windows.Media.Animation.StringAnimationUsingKeyFrames
            {
                Duration = new Duration(TimeSpan.FromSeconds(1.5)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            animation.KeyFrames.Add(new DiscreteStringKeyFrame(".", KeyTime.FromPercent(0.25)));
            animation.KeyFrames.Add(new DiscreteStringKeyFrame("..", KeyTime.FromPercent(0.5)));
            animation.KeyFrames.Add(new DiscreteStringKeyFrame("...", KeyTime.FromPercent(0.75)));
            animation.KeyFrames.Add(new DiscreteStringKeyFrame("", KeyTime.FromPercent(1.0)));

            Storyboard.SetTarget(animation, dotAnimation);
            Storyboard.SetTargetProperty(animation, new PropertyPath(TextBlock.TextProperty));
            storyboard.Children.Add(animation);
            storyboard.Begin();

            stack.Children.Add(dots);
            stack.Children.Add(dotAnimation);
            ChatMessages.Children.Add(stack);
            return stack;
        }
        private void ChatBotButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isChatMinimized)
            {
                ChatBotWindow.Height = 400;
                ChatHistory.Visibility = Visibility.Visible;
                _isChatMinimized = false;
                if (ChatMessages.Children.Count == 0) InitializeChat();
            }
            else
            {
                ChatBotWindow.Visibility = ChatBotWindow.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                if (ChatBotWindow.Visibility == Visibility.Visible && ChatMessages.Children.Count == 0)
                    InitializeChat();
            }
        }

        private void MinimizeChatButton_Click(object sender, RoutedEventArgs e)
        {
            ChatBotWindow.Height = 40;
            ChatHistory.Visibility = Visibility.Collapsed;
            _isChatMinimized = true;
        }

        private async void SendChatMessage_Click(object sender, RoutedEventArgs e)
            => await ProcessUserMessage();

        private async void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ProcessUserMessage();
            }
        }
        #endregion

        #region Логирование
        private void LogAction(string actionType)
        {
            int? userId = _currentReader?.ReaderId ?? _currentAuthor?.AuthorId;
            if (!userId.HasValue) return;

            using (var conn = new NpgsqlConnection(DbConnection.connectionStr))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand(
                        "INSERT INTO reading_sessions (reader_id, action_type, start_time) VALUES (@id, @action, NOW())",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("id", userId.Value);
                        cmd.Parameters.AddWithValue("action", actionType);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Logging error: {ex.Message}");
                }
            }
        }

        private void Logout(object sender, RoutedEventArgs e)
        {
            LogAction("logout");
            MainWindow.Logout();
            Close();
        }
        #endregion

    }
}