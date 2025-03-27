using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Npgsql;

namespace PartnerManagementForm
{
    public partial class Form1 : Form
    {
        // Стиль согласно руководству
        private readonly Color PrimaryBackgroundColor = Color.White;
        private readonly Color SecondaryBackgroundColor = Color.FromArgb(244, 232, 211); // #F4E8D3
        private readonly Color AccentColor = Color.FromArgb(103, 186, 128); // 67BA80
        private new readonly Font DefaultFont = new Font("Segoe UI", 9);
        private readonly Font HeaderFont = new Font("Segoe UI", 10, FontStyle.Bold);

        // Элементы интерфейса
        private Panel partnersPanel;
        private PictureBox logoPictureBox;

        // Подключение к базе данных
        private const string ConnectionString = "Host = localhost; Port = 5432; Database = demoex_practice; Username = postgres; Password = 123098";

        public Form1()
        {
            InitializeUIComponents();
            ConfigureFormAppearance();
            LoadPartnerData();
        }

        // Инициализация компонентов интерфейса
        private void InitializeUIComponents()
        {
            // Настройка логотипа
            logoPictureBox = new PictureBox
            {
                Image = Image.FromFile("D:/СПБКТ/Демоэкзамен практика/PartnerManagementForm/Resources/logo.png"),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(250, 80),
                Location = new Point(30, 20)
            };

            // Настройка панели партнёров
            partnersPanel = new Panel
            {
                BackColor = SecondaryBackgroundColor,
                AutoScroll = true,
                Location = new Point(30, 120),
                Size = new Size(840, 500),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Добавление элементов на форму
            this.Controls.Add(logoPictureBox);
            this.Controls.Add(partnersPanel);
        }

        // Настройка внешнего вида формы согласно руководству
        private void ConfigureFormAppearance()
        {
            this.Text = "Система управления партнёрами компании";
            this.Size = new Size(900, 700);
            this.BackColor = PrimaryBackgroundColor;
            this.Icon = new Icon("D:/СПБКТ/Демоэкзамен практика/PartnerManagementForm/Resources/app_icon.ico");
            this.Font = DefaultFont;
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        // Загрузка данных о партнёрах из базы данных
        private void LoadPartnerData()
        {
            try
            {
                List<Partner> partners = RetrievePartnersFromDatabase();
                DisplayPartners(partners);
            }
            catch (NpgsqlException ex)
            {
                ShowErrorMessage($"Ошибка подключения к базе данных: {ex.Message}");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Неожиданная ошибка: {ex.Message}");
            }
        }

        // Получение списка партнёров из PostgreSQL
        private List<Partner> RetrievePartnersFromDatabase()
        {
            var partners = new List<Partner>();

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                // SQL-запрос для получения данных о партнёрах и их продажах
                string query = @"SELECT p.id_partner, pt.partner_type, p.partner_name, p.director, p.phone, p.rating,
                    COALESCE((SELECT SUM(w.quantity * pr.min_cost_for_partner) FROM warehouse w JOIN product pr ON 
                    w.product_id = pr.id_product WHERE w.partner_id = p.id_partner), 0) AS total_sales from
                    partner p JOIN partner_type pt ON p.type_id = pt.id_partner_type ORDER BY p.partner_name";

                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        partners.Add(new Partner
                        {
                            Id = reader.GetInt32(0),
                            Type = reader.GetString(1),
                            Name = reader.GetString(2),
                            Director = reader.GetString(3),
                            Phone = reader.GetString(4),
                            Rating = reader.GetString(5),
                            TotalSales = Convert.ToDecimal(reader.GetDouble(6))
                        });
                    }
                }
            }

            return partners;
        }

        // Отображение списка партнёров на форме
        private void DisplayPartners(List<Partner> partners)
        {
            partnersPanel.Controls.Clear();

            int verticalPosition = 20;
            foreach (var partner in partners)
            {
                Panel partnerCard = CreatePartnerCard(partner, verticalPosition);
                partnersPanel.Controls.Add(partnerCard);
                verticalPosition += partnerCard.Height + 15;
            }
        }

        // Создание карточки партнёра
        private Panel CreatePartnerCard(Partner partner, int topPosition)
        {
            var card = new Panel
            {
                BackColor = PrimaryBackgroundColor,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(790, 120),
                Location = new Point(20, topPosition),
                Tag = partner.Id
            };

            // Расчёт скидки для партнёра
            int discountPercentage = CalculateDiscountPercentage(partner.TotalSales);

            // Заголовки карточки (Тип | Наименование)
            var headerLabel = new Label
            {
                Text = $"{partner.Type} | {partner.Name}",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = HeaderFont,
                ForeColor = AccentColor
            };

            // Процент скидки
            var discountLabel = new Label
            {
                Text = $"{discountPercentage}%",
                Location = new Point(700, 15),
                AutoSize = true,
                Font = HeaderFont,
                ForeColor = AccentColor,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            // Директор
            var directorLabel = new Label
            {
                Text = $"{partner.Director}",
                Location = new Point(15, 40),
                AutoSize = true,
                Font = DefaultFont
            };

            // Телефон
            var phoneLabel = new Label
            {
                Text = $"{partner.Phone}",
                Location = new Point(15, 65),
                AutoSize = true,
                Font = DefaultFont
            };

            // Рейтинг
            var ratingLabel = new Label
            {
                Text = $"Рейтинг: {partner.Rating}",
                Location = new Point(15, 90),
                AutoSize = true,
                Font = DefaultFont
            };

            // Добавление элементов на карточку
            card.Controls.Add(headerLabel);
            card.Controls.Add(discountLabel);
            card.Controls.Add(directorLabel);
            card.Controls.Add(phoneLabel);
            card.Controls.Add(ratingLabel);

            return card;
        }

        // Засчёт процента скидки на основе объёма продаж
        private int CalculateDiscountPercentage(decimal totalSales)
        {
            if (totalSales > 300000m) return 15;
            if (totalSales > 50000m) return 10;
            if (totalSales > 10000m) return 5;
            return 0;
        }

        // Показать сообщение об ошибке
        private void ShowErrorMessage(String message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Класс для хранения данных о партнёре
    public class Partner
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Director { get; set; }
        public string Phone { get; set; }
        public string Rating { get; set; }
        public decimal TotalSales { get; set; }
    }
}
