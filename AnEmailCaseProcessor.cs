/**
 *                  | Документация бизнес-логики |
 *
 * Бизнес-процесс запускается при добавлении email Обращения
 * 
 * Читается Контакт из Обращения и происходит проверка Контакта на спам по следующем принципам
 * 1. В Контакте поле "Тип" имеет значение "Клиент не определен/Спам"
 * 2. В системной настройке "OLP: Этап 1" выставлено значение "false"
 * 
 * Если Контакт подходит под описание, то нужно *Выставить 1 линию поддержки 
 * с полем "Категория" установленном на значение "Новая командировка/Изменение командировки"
 * После чего Бизнес-процесс завершается
 * 
 * Если же Контакт не спам, то мы получаем данные email-а (Заголовок, тело, кому, копии и т.п) 
 * из объекта родительской Активности и выполняем следующие действия
 * 1. Достаем за базы данных все ОСНОВНЫЕ и ДЕЖУРНЫЕ группы обслуживания
 * 2. На основе полей "Кому" и "Копии" в email-е отбираем подходящие ОСНОВНЫЕ и ДЕЖУРНЫЕ группы обслуживания 
 * 
 * Далее мы получаем email "Адрес" и "Домен" из объекта Контакт
 * Делаем проверку на наличие в заголовке строк "TRAVEL-" или "ЗАЯВКА ПО РЕЛОКАЦИИ_"
 * Первая указывает на заявку от компании Яндекс, вторая от Сибур, которые обрабатываются отдельно
 * В Обращение устанавливаем поле "Номер TRAVEL" или "Релокация - СИБУР" (В зависимости от темы письма) 
 * на "Номер темы", которую вытаскиваем из "Заголовка" письма
 * 
 * После чего мы проверяем заполнены ли поля "Компания" и "Тип" в объекте Контакт
 * Если "Компания" указана, а "Тип" Контакта "Сотрудник" или "Поставщик" и системная настройка "OLP: Этап 1" не установлена
 *     *Выставить 1 линию поддержки с полем "Категория" установленном на значение "Сотрудник/Поставщик"
 *     После чего Бизнес-процесс завершается
 * 
 * Если ни "Компания", ни "Тип" не указаны
 *     Тогда пробуем найти домен Контакта среди доменов компании или среди почт компании. Делаем мы это так
 *     Достаем за базы данных Контрагента из объект "Средство связи контрагента" по следующим фильтрам
 *     1. В колонках "Тип" значение "Почтовый домен", а в "Номер" значение "Домен email" из объекта Контакт
 *     Или
 *     2. В колонках "Тип" значение "Email", а в "Номер" значение "Адрес" email-а из объекта Контакт
 *     После чего получаем поля "Тип" и "Холдинг" Контрагента из соответствующего объекта
 * 
 *     Проверяем найдена ли компания по домену контакта
 *     Если удалось найти Контрагента, у которого в поле "Тип" значение "Поставщик" или "Наша компания"
 *         Устанавливаем в поля Контакта "Тип" и "Компанию" значения Контрагента и 
 *         "Поставщик" или "Сотрудник", соответственно
 *         Так же, если не установлена системная настройка "OLP: Этап 1", то нужно 
 *             *Выставить 1 линию поддержки и завершить Бизнес-процесс
 *         Далее логика из этой части будет описана ниже в **Переход 2
 *     
 *     Если Компания или холдинг найдены, то выполняем логику описанную ниже в **Переход 1
 * 
 * Если установлена системная настройка "OLP: Этап 1" и "Тип" Контакта "Сотрудник" или "Поставщик"
 *     Запоминаем, что "Категорию обращение" в последствии будет установлена как "Сотрудник/Поставщик"
 *     После чего выполняем логику описанную ниже в **Переход 2
 * 
 * Если "Компания" указана и "Тип" Контакта "Клиент" или "Клиент не определен", то выполняем логику описанную ниже в **Переход 1
 * 
 * **Переход 1 (ЕИС - поиск контакта по Ид./Email/Телефону 1)
 * Выполняем запрос сервис ЕИС - поиск контакта по Ид./Email/Телефону по методу Поиск контакта по Ид./Email/телефону
 * передавая параметр "Адрес" email-а Контакта.
 * 
 * Проверяем найден ли Контакт в ЕИС
 * Если пришел НЕ успешный ответ от ЕИС, но Контрагент пустой и "Тип" Контакта пустой или "Клиент не определен/Спам"
 *     Категория обращения будет помечена как "Клиент не определен/Спам"
 *     Установка в объекте Контакт полей "Компания" и "Тип" на "OLP:AC Компания - не определена" и "Клиент не определен/Спам"
 *     
 *     Далее, Если системная настройка "OLP: Этап 1" не установлена
 *     то *Выставить 1 линию поддержки с "Категория обращения" как "Клиент не определен/Спам" и завершить Бизнес-процесс
 *     
 *     Eсли же установлена системная настройка "OLP: Этап 1", то
 *     установить в объекте Обращение полей "Категория", "Важность" и "Срочность"
 *     на "Клиент не определен/Спам", "Не важно" и "Не срочно"
 *     
 *     Последующая логика описана ниже в **Переход 2 
 * 
 * Если просто пришел успешный ответ от ЕИС или пришел успешный ответ от ЕИС и поле "Подтвержден в БД АК" Контакта установлено
 *     Получаем поля "Пилот" и "Холдинг" из объекта Контрагент, 
 *     у которого "Внешний Ид.БД АК" совпадает с "CompanyId" пришедшего из ЕИС
 * 
 *     Далее, для каждого email-а из списка email-ов, пришедших от ЕИС
 *         Выполняем запрос в базу данных по объекту "Средство связи контакта", у которого выставлены фильтры
 *         1. Поле "Contact" совпадает с Контактом
 *         2. Поле "Number" совпадает с "Адресом" email-а
 *         3. Поле "CommunicationType" или же тип связи выставлен как "Email"
 * 
 *         Если такого объекта нет, то добавляем его с вышеперечисленными полями
 * 
 *     То же самое действия проделываем с телефонами, пришедшими от ЕИС, заменяя параметр тип связи на "Phone"
 * 
 *     Обновляем данные Контакта
 * 
 *     Устанавливаем параметр "Ид. Холдинга компании клиента" на Контрагента для последующей установки параметра
 * 
 *     Последующая логика описана ниже в **Переход 3
 * 
 * Во всех остальных случаях
 *     Проверяем есть ли уже Компания
 *     Если Компания не установлена 
 *         Обновляем объект Контакт, устанавливая "Тип" на "Клиент", "Email" на "email" Контакта и "Компания" на "Контрагента"    
 *     Если Компания установлена 
 *         Обновляем объект Контакт, устанавливая "Тип" на "Клиент" и "Email" на "email" Контакта
 *     Последующая логика описана ниже в **Переход 2
 * 
 * **Переход 2 (Найти данные компании привязанной к контакту ненайденного в ЕИС)
 * Считываем объект Контрагент, у которого поле "Id" совпадает с объектом Обращение
 * После чего выставляем "Ид. Холдинга компании клиента" как "Холдинг" из объекта Контрагент
 * Последующая логика описана ниже в **Переход 3
 * 
 * **Переход 3 Чтение карточки контакта после обновления
 * Чтение объект Контакта после обновления данных
 * Выставляем в контекста процесса (Запоминяем для последующей установки в объект) параметры 
 * "Клиент ВИП платформа" на "Признак ВИП платформы",
 * "Признак ВИП" на "Признак ВИП",
 * "Ид. Компании клиента" на "Компания",
 * а так же в объект родительскую Активность выставляем "Контрагента" на "Компания" (Все параметры берем у Контакта)
 * 
 * Делаем запрос в бд по объекту "Группы обслуживания", добавляя следующие фильтры, 
 * если у Контакта не установлен параметр "ВИП платформа"
 * "Тип группы", указав выборку по "Основная группа"
 * "Почтовый ящик", указав выборку по "Почтовый ящик для регистрации обращений"
 * А так же сделать выборку "Компания" у контакта или "Холдинг" у Контрагента по параметру
 * "Компании" у объекта "Компании в группе обслуживания"
 * Если же у Контакта "ВИП платформа" установлена, то только добавить фильтр "Тип группы" на "ВИП Платформа" 
 * 
 * Далее для каджого элемента получившейся коллекции добавляем в список "Коллекция групп обслуживания"
 * новый элеметр у которого буду установлены параметры "Ид. Компании клиента" и "График работы", которые берем из ГО
 * Если нет ни одной подходящей ГО, установить параметр "Дежурную группу"
 * 
 * После чего идет проверка найдена ли группа по компании ВИП Платформа
 * Если не установлены параметр "Дежурная группа" и системная настройка "OLP: Этап 1"
 *     *Найти ГО основную по графику работы
 *     Далее логика описывается в **Переход 4
 * 
 * Если группа по компании ВИП Платформа не найдена, то 
 *     сразу переходим к логике в **Переход 4
 * 
 * Если установлена системная настройка "OLP: Этап 1"
 *     *Найти ГО основную по графику работы
 *     
 *     Какую ГО установить?
 *     Если установлены "Ид. экстра группы из кому/копии" и "Дежурная группа", а 
 *     "Ид. основной группы по почтовому ящику" не стоит
 *         Далее логика описывается в **Переход 5
 * 
 *     Если не установлена "Дежурная группа"
 *         (Отправить автоответ - не пишите на бук)
 *         В Активности установить поле Автоматическое уведомление
 *         Последующая логика описывается в **Переход 6
 * 
 *     Если установлены "Ид. основной группы по почтовому ящику" и "Дежурная группа"
 *         Если указаны "Ид. экстра группы из кому/копии" и "Дежурная группа"
 *             Далее логика описывается в **Переход 5
 * 
 *         Если установлена "Ид. выбранной ГО" и не стоит "Дежурная группа"
 *             Последующая логика описывается в **Переход 6
 * 
 *         Если не подходит осн по компании и та что указана в почте и нет дежурной в копии
 *             (Найти ГО дежурную по графику работы) вредо как по *Найти ГО основную по графику работы 
 * 
 *             Если не установлено "Ид. выбранной ГО" и
 *             Указана "Дежурная группа" или не указана и "Email" Контакта содержит "NOREPLY@" или "NO-REPLY@" или "EDM@npk.team"
 *                 Последующая логика описывается в **Переход 6
 * 
 *             Если не установлена "Дежурная группа" и "Email" Контакта не содержит "NOREPLY@" или "NO-REPLY@" или "EDM@npk.team"
 *                 Отправить автоответ на букинг
 *                 В Активности установить поле Автоматическое уведомление
 *                 Последующая логика описывается в **Переход 6
 * 
 *     В противном случае **Переход 7
 * 
 * **Переход 4 (Какая ГО должна быть у контакта?)
 * Если установлены параметры "Дежурная группа" и "Клиент ВИП", то 
 *     установить в "Ид. выбранной ГО" значение "OLP:ГО Дежурная группа" и **Переход 5
 * 
 * Если не стоит "Дежурная группа", то **Переход 5
 * 
 * Если установлен параметр "Дежурная группа", но не "Клиент ВИП"
 *     установить в "Ид. выбранной ГО" значение "OLP:ГО Общая 1 линия поддержки"
 *     *Добавить 1 линия поддержки на обр
 *     **Переход 7
 * 
 * **Переход 5 (Найти ГО дежурную из кому/копии по графику работы)
 * (Найти ГО дежурную из кому/копии по графику работы) Похоже на *Найти ГО основную по графику работы с заменой "Основной", а "Дежурную"
 * 
 * Если Дежурная ГО найдена, то **Переход 6
 * 
 * Если Дежурной ГО нет
 *     *Найти основную ГО для контакта по компаниям
 *     Если Дежурной ГО нет
 *         То установить "Ид. выбранной ГО" на "Ид. экстра группы из кому/копии"
 *     (Отправить автоответ - не пишите на бук)
 *     В Активности установить поле Автоматическое уведомление
 *     Последующая логика описывается в **Переход 6
 * 
 * **Переход 6 (Найти Группу обслуживания по Ид)
 * Читаем объект Группы обслуживания, которуя раньше выудили из проверок, она находиться в "Ид. выбранной ГО"
 * Если "Тип" Группы обслуживания это "ВИП Платформа"
 *     Устанавливаем "Важность для выставления" как "Важно"
 *     *Добавить 2 линия поддержки на обр
 *     **Переход 7
 * 
 * Если установлены "Клиент ВИП" у ГО
 *     Устанавливаем "Важность для выставления" как "Важно"
 *     Если "Распределение ВИП" у ГО
 *         (Найти 3 линию поддержки для обращения (старший агент))
 *         Если нашли *Добавить 3 линия поддержки на обр и **Переход 7
 *     Далее *Добавить 2 линия поддержки на обр и **Переход 7
 * 
 * (Найти 1 линию поддержки для обращения)
 * Если "Приоритет" у родительской Активности "Высокий" - Установить "Важность для выставление" как "Важно"
 * 
 * Если найдена 1 линия поддержки - *Добавить 2 линия поддержки на обр и **Переход 7
 * 
 * Если не найдена 1 линия поддержки и установлен "OLP: Этап 1"
 *     установить в "Ид. выбранной ГО" значение "OLP:ГО Общая 1 линия поддержки"
 *     *Добавить 1 линия поддержки на обр
 *     **Переход 7
 * 
 * **Переход 7
 * Если не успешный ответ от ЕИС, то завершить БП
 * Если "OrderNumbCheck" от ЕИС не пустой
 *     (Собрать услуги для добавления)
 *     Запустить "OLP: Подпроцесс - Обновление услуг контакта v 3.0.1"
 * Завершить БП
 * 
 * *Выставить 1 линию поддержки
 * Изменить в объекте Обращение поля:
 * 1. "Категория" на ту категорию, которая установлена в данном контексте 
 * (по умолчанию "Новая командировка/Изменение командировки")
 * 2. "Группа" обслуживания на "OLP:ГО Общая 1 линия поддержки"
 * 3. "Линия" поддержки на "OLP:ОР 1 линия поддержки"
 * 4. "Важность" на "Не важно"
 * 5. "Срочность" на "Не срочно"
 * 
 * *Найти ГО основную по графику работы 
 * Если "График работы" установлен как "Круглосуточный" сразу запоминаем ГО и идем дальше, пропуская шаги ниже 
 * Для каждого элемента Коллекции групп обслуживания проверяем следующие условия
 * 1. Если дата обращение совпадает с выходным-праздничным днем, то переходим к следующей ГО
 * 2. Если дата обращение совпадает с праздничным днем, который рабочий, то записать текущую ГО
 * 3. Если дата обращение подходит под график работы текущей ГО, то используем эту конкрутную ГО
 * в противном случаем переходим на "Дежурную групп"
 * 
 * *Найти основную ГО для контакта по компаниям
 * Делаем запрос в бд по объекту "Группы обслуживания", добавляя следующие фильтры, 
 * если у Контакта не установлен параметр "ВИП платформа"
 * "Тип группы", указав выборку по "Основная группа"
 * "Почтовый ящик", указав выборку по "Почтовый ящик для регистрации обращений"
 * А так же сделать выборку "Компания" у контакта или "Холдинг" у Контрагент по параметру
 * "Компании" у объекта "Компании в группе обслуживания"
 * Если же у Контакта "ВИП платформа" установлена, то только добавить фильтр "Тип группы" на "ВИП Платформа"
 * 
 * *Добавить 1 линия поддержки на обр
 * Изменить Обращение в параметрах
 * 1. "Линия поддержки" на "OLP:ОР 1 линия поддержки"
 * 2. "Важность" на "Важность для выставления" (из контекста текущего разветвления)
 * 3. "Срочность" на "Не срочно"
 * 4. "Признак ВИП у Автора" на "Клиент ВИП" (из контекста текущего разветвления)
 * 5. "Группа обслуживания" на "Ид. выбранной ГО"
 * 6. "Компания" на "Компания" у Контакта
 * 7. "Категория" на "Категория обращения" (из контекста текущего разветвления)
 * 8. "Группа обслуживания (основная для очереди)" на "Группа обслуживания для выставления очереди" (из контекста текущего разветвления) 
 * 
 * *Добавить 2 линия поддержки на обр
 * Изменить Обращение в параметрах как в *Добавить 1 линия поддержки на обр, но
 * "Линия поддержки" на "OLP:ОР 2 линия поддержки"
 * 
 * *Добавить 3 линия поддержки на обр
 * Изменить Обращение в параметрах как в *Добавить 1 линия поддержки на обр, но
 * "Линия поддержки" на "OLP:ОР 2 линия поддержки"
 * 1. "Линия поддержки" на "OLP:ОР 3 линия поддержки"
 * 2. "Агент" на "Старший агент" 3-й линии
 *
 * */
namespace AnseremPackage
{
    using System;
    using System.IO;
    using System.Net;
    using System.Collections.Generic;
    using System.Text;
    using global::Common.Logging;
    using Terrasoft.Core;
    using Terrasoft.Configuration;
    using Terrasoft.Common;
    using Terrasoft.Core;
    using Terrasoft.Core.DB;
    using Terrasoft.Core.Entities;
    using Terrasoft.Core.Process;
    using Terrasoft.Core.Entities.Events;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Text.RegularExpressions;
    using System.Runtime; 
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;

    [EntityEventListener(SchemaName = nameof(Case))]
    public class AnEmailCaseProcessor: BaseEntityEventListener
    { 
        public static readonly ILog _log = LogManager.GetLogger("EmailCaseProcessor");

        /**CONSTS**/
        public UserConnection _userConnection { get; set; }

        public Guid SERVICE_GROUP_TYPE_MAIN = new Guid("C82CA04F-5319-4611-A6EE-64038BA89D71");

        public Guid SERVICE_GROUP_TYPE_EXTRA = new Guid("DC1B5435-6AA1-4CCD-B950-1C4ADAB1F8AD");

        public Guid CONTACT_TYPE_SUPPLIER = new Guid("260067DD-145E-4BB1-9C91-6BF3A36D57E0");

        public Guid CONTACT_TYPE_EMPLOYEE = new Guid("60733EFC-F36B-1410-A883-16D83CAB0980");

        public Guid CONTACT_TYPE_CLIENT = new Guid("00783ef6-f36b-1410-a883-16d83cab0980");

        public Guid OLP_DUTY_SERVICE_GROUP = new Guid("3A8B2C01-7A4D-4D5D-A7EA-8563BF6220B9"); 

        // OLP:ГО Общая 1 линия поддержки
        // public Guid OLP_GENERAL_FIRST_LINE_SUPPORT = new Guid("64833178-8B17-4BB6-8CD9-6165B9B82637"); // TODO PROD id?
        public Guid OLP_GENERAL_FIRST_LINE_SUPPORT = new Guid("9FD6B5C6-3BEF-42A0-83AD-CF2FB39E4D1B"); 

        // OLP:ОР 1 линия поддержки
        // public Guid OLP_OR_FIRST_LINE_SUPPORT = new Guid("B401FC39-77E4-4B53-985F-21E68947A107"); // TODO PROD id?
        public Guid OLP_OR_FIRST_LINE_SUPPORT = new Guid("D620B5ED-AB7C-487C-8B0A-B68C9621BFF6"); 
        
        // OLP:ОР 2 линия поддержки
        public Guid OLP_OR_SECOND_LINE_SUPPORT = new Guid("DF594796-8E36-41BC-8EDD-732967053947"); 

        // OLP:ОР 3 линия поддержки (старшие агенты)
        public Guid OLP_OR_THIRD_LINE_SUPPORT = new Guid("3D0C8864-BF2F-4734-8A29-31873EB07440"); 

        public Guid CASE_URGENCY_TYPE_NOT_URGENT = new Guid("7a469f22-111d-4749-b5c2-e2a109a520a0");

        public Guid CASE_URGENCY_TYPE_URGENT = new Guid("97c567ad-dbf8-4923-a766-c49a85b3ebdf");

        public Guid CASE_IMPORTANCY_IMPOTANT = new Guid("fd6b8923-4af8-48f9-8180-b6e1da3a1e2d");

        public Guid CASE_IMPORTANCY_NOT_IMPOTANT = new Guid("007fc788-5edd-42dd-a9ac-c56d010e7205");

        public Guid VIP_PLATFORM = new Guid("97c567ad-dbf8-4923-a766-c49a85b3ebdf");

        public Guid ACTIVITY_PRIORITY_HIGH = new Guid("D625A9FC-7EE6-DF11-971B-001D60E938C6");

        public Guid ACTIVITY_PRIORITY_MEDIUM = new Guid("AB96FA02-7FE6-DF11-971B-001D60E938C6");

        public Guid ACTIVITY_PRIORITY_LOW = new Guid("AC96FA02-7FE6-DF11-971B-001D60E938C6");

        public Guid CONTACT_TYPE_UNDEFINED_CLIENT_SPAM = new Guid("1a334238-08ba-466d-8d40-a996afcb8fe1");

        public Guid CASE_CATEGORY_EMPLOYEE_SUPPLIER = new Guid("84f67e2e-842e-47ae-99aa-882d1bc8e513");

        public Guid ACCOUNT_TYPE_SUPPLIER = new Guid("1414f55f-21d2-4bb5-847a-3a0681d0a13a");

        public Guid ACCOUNT_TYPE_OUR_COMPANY = new Guid("57412fad-53e6-df11-971b-001d60e938c6");
        
        public Guid CASE_STATUS_CLOSED = new Guid("ae7f411e-f46b-1410-009b-0050ba5d6c38");
        
        public Guid CASE_STATUS_CANCELED = new Guid("6e5f4218-f46b-1410-fe9a-0050ba5d6c38");
 
        public Guid COMMUNICATION_TYPE_EMAIL_DOMAIN = new Guid("9E3A0896-0CBE-4733-8013-1E70CB09800C");
        
        public Guid COMMUNICATION_TYPE_EMAIL = new Guid("EE1C85C3-CFCB-DF11-9B2A-001D60E938C6");
        
        public Guid ACTIVITY_TYPE_EMAIL = new Guid("E2831DEC-CFC0-DF11-B00F-001D60E938C6");
        
        public Guid OLP_AC_COMPANY_IS_NOT_DEFINED = new Guid("A1998F90-2EEC-48A4-94E8-A3CF48134FFB");
        /**CONSTS**/
 
        /**SYS_SETTINGS**/
        public bool isOlpFirstStage { get; set; }

        public bool LoadingCheck { get; set; }
        /**SETTINGS**/
        
        /**PARAMS**/
        public Guid caseCategory { get; set; }

        public Contact contact { get; set; }
        
        public Guid contactId { get; set; }

        public Activity activity { get; set; }

        public Profile eis { get; set; }

        public Account account { get; set; }

        public Guid caseId { get; set; }

        public Guid parentActivityId { get; set; }

        public Guid mainServiceGroup { get; set; }

        public Guid extraServiceGroup { get; set; }

        public Guid holding { get; set; }

        public Guid clientCompanyId { get; set; }

        public Guid selectedServiceGroupId { get; set; }

        public bool clientVipPlatform { get; set; }

        public bool clientVip { get; set; }

        public bool isExtraServiceGroup { get; set; }

        public Guid importancy { get; set; }

        public Guid urgency { get; set; }
        
        public string email { get; set; }
        
        public string copies { get; set; }
        
        public string mailto { get; set; }

        public string theme { get; set; }

        public bool isResponseSuccessful { get; set; }
        
        public Guid firstLineSupport { get; set; }
        /**PARAMS**/

        /**LEGACY**/
        public Guid MainGroupIdByEmail { get; set; }
        
        public string MainGroupEmailBox { get; set; }
        
        public Guid MainGroupEmailBoxId { get; set; }
        
        public Guid MainEmailBoxIdForReg { get; set; }
        
        public string MainSheduleTypeByMail { get; set; }
        
        public string ExtraGroupEmailBox { get; set; }
        
        public Guid ExtraGroupIdByEmail { get; set; }
        
        public Guid ExtraGroupEmailBoxId { get; set; }
        
        public string ExtraSheduleTypeByMail { get; set; }
        
        public Guid ContactIdForEmailAndPhone { get; set; }
        
        public List<ServiceGroupElement> ProcessSchemaParameterServiceGroupCollection { get; set; }
         
        public bool ProcessSchemaParameterIsDutyGroup { get; set; }
        
        public Guid ProcessSchemaParamServiceGroupId { get; set; }

        public int ProcessSchemaParameter2 { get; set; }
        
        public int ProcessSchemaParameter3 { get; set; }

        public string DateFromForReply { get; set; }
        
        public string  DateToForReply { get; set; }
        
        public Guid ServiceGroupForOrder { get; set; }
 
        public Guid ProcessSchemaParamCompanyFoundId { get; set; }

        public Guid ProcessSchemaParamHoldingFoundId { get; set; }

        public CompositeObjectList<CompositeObject> ServicesToAdd { get; set;}
        
        public string ProcessSchemaParamTime { get; set;}
        /**LEGACY**/
        
		public Entity _case { get; set; }

        public override void OnInserted(object sender, EntityAfterEventArgs e)
        {
 			_log.Info("\n\nSTART");
 			base.OnInserted(sender, e);
            _case = (Entity)sender;
           
            // DEFAULT: Новая командировка/Изменение командировки
            caseCategory = new Guid("1b0bc159-150a-e111-a31b-00155d04c01d"); 

            caseId = _case.GetTypedColumnValue<Guid>("Id");
            _log.Info("CaseId: " + caseId);

            // Чтение карточки контакта из обращения
            isOlpFirstStage = true;
            LoadingCheck = true;
            _userConnection = _case.UserConnection;

            contactId = _case.GetTypedColumnValue<Guid>("ContactId");
            contact = ReadContactFromCase(contactId);

            _log.Info("contactId: " +  contactId);

            // Нет (СПАМ) - 2 ЭТАП
            if (contact.GetTypedColumnValue<Guid>("TypeId") == CONTACT_TYPE_UNDEFINED_CLIENT_SPAM) 
            {
                _log.Info("Contact is spam");
                
                // Спам на обращении + 1 линия поддержки
                UpdateCaseToFirstLineSupport();
                return;
            }

            parentActivityId = _case.GetTypedColumnValue<Guid>("ParentActivityId");
            _log.Info("parentActivityId: " + parentActivityId);

            // email родительской активности
            activity = GetParentActivityFromCase();
            try
            {
                email = activity.GetTypedColumnValue<string>("Sender");

                mailto = activity.GetTypedColumnValue<string>("Recepient"); 

                copies = activity.GetTypedColumnValue<string>("CopyRecepient"); 

                theme = activity.GetTypedColumnValue<string>("Title"); 
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            _log.Info("email: " + email + "; mailto: " + mailto + "; copies: " + copies + "; title: " + theme);

            /**
             * Чтение всех основных групп для выделения подходящей основной группы
             * Найти основную группу по email в кому/копия
             */
            mainServiceGroup = GetServiceGroupMain();

            /**
             * Чтение дежурной группы
             * Найти дежурную группу по email в кому/копия
             */
            extraServiceGroup = GetServiceGroupExtra();
            
            _log.Info("mainServiceGroup: " + mainServiceGroup + "; extraServiceGroup: " + extraServiceGroup);

            /**
             * Добавить TRAVEL
             * Поставить отменено на всех отмененных тревел обращениях 
             */  
            SetTravelParameter(); 

            /**
             * Добавить Релокация - СИБУР
             * Поставить отменено на всех отмененных тревел обращениях - Копия 
             */
            SetSiburParameter(); 

            // Да (Сотрудник) - 2 ЭТАП
            if (contact.GetTypedColumnValue<Guid>("AccountId") != Guid.Empty && !isOlpFirstStage && 
                    (contact.GetTypedColumnValue<Guid>("TypeId") == CONTACT_TYPE_EMPLOYEE || contact.GetTypedColumnValue<Guid>("TypeId") == CONTACT_TYPE_SUPPLIER))
            {
                _log.Info("Employee, second stage");

                caseCategory = CASE_CATEGORY_EMPLOYEE_SUPPLIER; 
                /**
                 * Категория (Сотрудник/Поставщик) -2 этап
                 * Выставить 1 линию поддержки
                 */
                UpdateCaseToFirstLineSupport();
                return;
            }

            // 1 Этап (Сотрудник/Поставщик)
            if (isOlpFirstStage && (contact.GetTypedColumnValue<Guid>("TypeId") == CONTACT_TYPE_EMPLOYEE || contact.GetTypedColumnValue<Guid>("Type") == CONTACT_TYPE_SUPPLIER))
            {
                _log.Info("First stage, Employee or supplier");

                caseCategory = CASE_CATEGORY_EMPLOYEE_SUPPLIER;
                goto2();
            }

            // Нет (Новый)
            if (contact.GetTypedColumnValue<Guid>("Account") == Guid.Empty || contact.GetTypedColumnValue<Guid>("TypeId") == Guid.Empty)
            {
                _log.Info("Not, new");

                account = FetchAccountById(GetAccountIdFromAccountCommunication());

                // Да (Поставщик)
                if (account != null && account.GetTypedColumnValue<Guid>("TypeId") == ACCOUNT_TYPE_SUPPLIER)
                {
                    _log.Info("Yes, supplier");

                    caseCategory = CASE_CATEGORY_EMPLOYEE_SUPPLIER;
                    SetContactType(contact.GetTypedColumnValue<Guid>("Id"), CONTACT_TYPE_SUPPLIER);
                    if (isOlpFirstStage)
                    {
                        _log.Info("OlpFirstStage");

                        goto2();
                    }
                    else
                    {
                        _log.Info("Not OlpFirstStage");
                        UpdateCaseToFirstLineSupport();
                    }
                }

                // Да (Аэроклуб)
                if (account != null && account.GetTypedColumnValue<Guid>("TypeId") == ACCOUNT_TYPE_OUR_COMPANY)
                {
                    _log.Info("Yes, aerolcub");

                    caseCategory = CASE_CATEGORY_EMPLOYEE_SUPPLIER;

                    SetContactType(contact.GetTypedColumnValue<Guid>("Id"), CONTACT_TYPE_EMPLOYEE);
                    
                    if (isOlpFirstStage)
                    {
                        _log.Info("OlpFirstStage");
                        goto2();
                    }
                    else
                    {
                        _log.Info("Not OlpFirstStage");
                        UpdateCaseToFirstLineSupport();
                    }
                }

                // Да (Компания/Холдинг) или потенциальный СПАМ
                EisPath();
            }

            // Да (Клиент, СПАМ)
            if (contact.GetTypedColumnValue<Guid>("Account") != Guid.Empty && 
                    (contact.GetTypedColumnValue<Guid>("TypeId") == CONTACT_TYPE_CLIENT || contact.GetTypedColumnValue<Guid>("Type") == CONTACT_TYPE_UNDEFINED_CLIENT_SPAM))
            {
                _log.Info("Yes, client or spam");
                EisPath();
            }
            _log.Info("DONE");
            return;
        }

        public void EisPath()
        {
            isResponseSuccessful = SendEisRequest();
            _log.Info("isResponseSuccessful: " + isResponseSuccessful);
            // Да
            if (isResponseSuccessful || (isResponseSuccessful && contact.GetTypedColumnValue<bool>("aeroclubCheck")))
            {
                _log.Info("EISpath: Yes");

                account = FetchAccountByEis(new Guid(eis.Company.Id));

                RefreshEmailsAndPhones();

                // RefreshContact(account.GetTypedColumnValue<Guid>("Id"), contact.GetTypedColumnValue<Guid>("Id"));
                RefreshContact();

                holding = account.GetTypedColumnValue<Guid>("OlpHolding");

                // Чтение карточки контакта после обновления
                ReadContactAfterRefreshing();
            }

            // Нет по домену и нет по ЕИС и (пустой тип или СПАМ)
            if (isResponseSuccessful && contact.GetTypedColumnValue<Guid>("Account") == Guid.Empty && (contact.GetTypedColumnValue<Guid>("TypeId") == Guid.Empty || contact.GetTypedColumnValue<Guid>("TypeId") == CONTACT_TYPE_UNDEFINED_CLIENT_SPAM))
            {
                _log.Info("EISpath: Not by domain, not by eis and empty or spam");

                // категория СПАМ
                caseCategory = CONTACT_TYPE_UNDEFINED_CLIENT_SPAM;
                if (isOlpFirstStage)
                {
                    _log.Info("OlpFirstStage");
                    SetSpamOnCase();
                    goto2();
                }
                else
                {
                    _log.Info("Not OlpFirstStage");
                    SetSpamOnCase();   
                    return;
                }
            }

            // Нет по ЕИС
            // Да
            if (contact.GetTypedColumnValue<Guid>("AccountId") != Guid.Empty)
            {
                _log.Info("Not by eis: Yes");

                // Актуализировать тип контакта + Email 
                RefreshContactTypeAndEmail();
            }
            // Нет
            else
            {
                _log.Info("Not by eis: Not");
                // Актуализировать компанию контакта + Email
                RefreshContactCompanyAndEmail();
            }

            goto2();
        }

        public void goto2()
        {
            _log.Info("goto2");

            // Найти данные компании привязанной к контакту ненайденного в ЕИС
            // Выставить холдинг компании контакта
            holding = GetHoldingFromAccountBindedToEis();            
            
            ProcessSchemaParamHoldingFoundId = holding;

            // Чтение карточки контакта после обновления
            ReadContactAfterRefreshing();
        }

        /** 
         * Чтение карточки контакта после обновления
         */
        public void ReadContactAfterRefreshing()
        {
            _log.Info("ReadContactAfterRefreshing");

            GetContactAfterRefreshing(contact.GetTypedColumnValue<Guid>("Id"));

            clientVip = contact.GetTypedColumnValue<bool>("OlpSignVip");

            clientVipPlatform = contact.GetTypedColumnValue<bool>("OlpSignVipPlatf");

            clientCompanyId = contact.GetTypedColumnValue<Guid>("AccountId");

            ProcessSchemaParamCompanyFoundId = clientCompanyId;
            
            _log.Info("clientVip: " + clientVip + "; clientVipPlatform: " + clientVipPlatform + "; clientCompanyId: " + clientCompanyId);
            AddAccountToEmail();

            // Найти основную ГО для контакта по компаниям и ВИП Платформа
            GetMainServiceGroup();

            _log.Info("Is service group found by company vip platform");
            // Найдена ли группа по компании ВИП Платформа?
            // Да
            if (!isExtraServiceGroup && !isOlpFirstStage)
            {
                _log.Info("Yes");

                // Найти ГО основную по графику работы 
                GetMainServiceGroupBasedOnTimetable(); 
                goto4();
            }

            // Этап 1
            if (isOlpFirstStage)
            {
                _log.Info("First stage");

                // Есть ГО
                if (!isExtraServiceGroup)
                {
                    _log.Info("Service group is found");

                    // Найти ГО  основную по графику работы 1 этап
                    GetMainServiceGroupBasedOnTimetableOlpFirstStage(); 
                }

                // Какую ГО установить?
                _log.Info("Which service group to set");

                // Указана только дежурная в кому/копии
                if (MainGroupIdByEmail == Guid.Empty && ExtraGroupIdByEmail != Guid.Empty && isExtraServiceGroup)
                {
                    _log.Info("Only duty group was in to/copy");
                    goto5();
                }

                // Найдена ГО по компании и графику клиента и почтовому адресу
                if (!isExtraServiceGroup)
                {
                    _log.Info("Service group was found by company and timetable and email addres");
                    SendBookAutoreply(); // TODO Переделать под кастомные автоответы!
                    SetAutonotification();
                    goto6();
                }

                // Указана осн ГО в кому/копии
                if (mainServiceGroup == Guid.Empty && isExtraServiceGroup)
                {
                    _log.Info("Main service group in to/copy");

                    // Найти осн из почты по графику
                    GetMainServiceGroupFromMainBasedOnTimeTable(); 
                    if (isExtraServiceGroup && ExtraGroupIdByEmail != Guid.Empty)
                    {
                        goto5();
                    }
                    if (selectedServiceGroupId != Guid.Empty && extraServiceGroup == Guid.Empty)
                    {
                        goto6();
                    }
                    else
                    {
                        // Найти ГО дежурную по графику работы
                        GetExtraServiceGroupBaseOnTimetable();
                        
                        if (selectedServiceGroupId != Guid.Empty && (extraServiceGroup == Guid.Empty || contact.GetTypedColumnValue<string>("Email").Contains("NOREPLY@") || contact.GetTypedColumnValue<string>("Email").Contains("NO-REPLY@") || contact.GetTypedColumnValue<string>("Email").Contains("EDM@npk.team")))
                        {
                            goto6();
                        }
                        else if (selectedServiceGroupId == Guid.Empty && (!contact.GetTypedColumnValue<string>("Email").Contains("NOREPLY@") && !contact.GetTypedColumnValue<string>("Email").Contains("NO-REPLY@") && !contact.GetTypedColumnValue<string>("Email").Contains("EDM@npk.team")))
                        {
                            SendBookAutoreply(); // TODO Переделать под кастомные автоответы!
                            SetAutonotification();
                            goto6();
                        }
                    }
                }
                else
                {
                    goto7();
                    return;
                }
            }
            else // Нет 
            {
                goto4();
            }
            return;
        }

        public void goto4()
        {
            _log.Info("goto4");
            // Дежурная ГО 2 линия поддержки
            if (extraServiceGroup != Guid.Empty && clientVip)
            {
                _log.Info("Duty group second line of support");
                selectedServiceGroupId = OLP_DUTY_SERVICE_GROUP;
                goto5();
            }

            // Основная клиентская/ВИП Платформа
            if (extraServiceGroup == Guid.Empty)
            {
                _log.Info("Main clients/vip platform");
                goto5();
            }

            // Общая 1 линия поддержки
            if (extraServiceGroup != Guid.Empty && clientVip)
            {
                _log.Info("General first line of support");
                selectedServiceGroupId = OLP_GENERAL_FIRST_LINE_SUPPORT;
                SetFirstLineSupport();
                goto7();
            }
        }

        public void goto5()
        {
            _log.Info("goto5");

            // Найти ГО дежурную из кому/копии по графику работы
            GetExtraServiceGroupFromFromAndCopyBaseOnTimetable();
            
            // дежурная ГО найдена или есть основная почта
            if (selectedServiceGroupId != Guid.Empty)
            {
                goto6();
            }
            else
            {
                // Найти основную ГО для контакта по компаниям
                GetMainServiceGroupForContactBasedOnCompany(); 

                selectedServiceGroupId = selectedServiceGroupId == Guid.Empty ? ExtraGroupIdByEmail : selectedServiceGroupId;
                
                SendBookAutoreply(); // TODO Переделать под кастомные автоответы!
                SetAutonotification();
                goto6();
            }
        }

        public void goto6()
        {
            _log.Info("goto6");

            var serviceGroup = GetServiceGroupBySelectedId();

            // Выбранная ГО ВИП Платформа?
            // Да
            if (serviceGroup.GetTypedColumnValue<Guid>("OlpTypeGroupServiceId") == VIP_PLATFORM)
            {
                importancy = CASE_IMPORTANCY_IMPOTANT;
                SetSecondLineSupport();
                goto7();
            }

            // Клиент явдяется ВИП
            // Да 
            if (clientVip)
            {
                importancy = CASE_IMPORTANCY_IMPOTANT;

                if (serviceGroup.GetTypedColumnValue<bool>("OlpDistribution"))
                {
                    var thirdLineSupport = GetThirdLineSupport(serviceGroup.GetTypedColumnValue<Guid>("OlpOrgRoleId")); 
                    if (thirdLineSupport != Guid.Empty)
                    {
                        SetThirdLineSupport();
                        goto7();
                    }
                }
                SetSecondLineSupport();
                goto7();
            }

            // Найти 1 линию поддержки для обращения
            var firstLineSupport = GetFirstLineSupport(serviceGroup.GetTypedColumnValue<Guid>("OlpOrgRoleId"));

            if (activity.GetTypedColumnValue<Guid>("PriorityId") == ACTIVITY_PRIORITY_HIGH)
            {
                importancy = CASE_IMPORTANCY_IMPOTANT;
                _log.Info("importancy : " + importancy);
            }

            if (firstLineSupport != Guid.Empty)
            {
                SetSecondLineSupport();
                goto7();
            }
            if (firstLineSupport == Guid.Empty && isOlpFirstStage)
            {
                selectedServiceGroupId = OLP_GENERAL_FIRST_LINE_SUPPORT;
                _log.Info("selectedServiceGroupId: " + selectedServiceGroupId);
                SetFirstLineSupport();
                goto7();
            }
        }

        /**
         * TODO
         * */
        public void goto7()
        {
            _log.Info("goto7");
            // if (isResponseSuccessful)
            // {
            //     
            //     _log.Info("Eis success");
            //     return;
            // }
       
            // if (new Guid(eis.OrderNumbCheck) != Guid.Empty) 
            // {
            //     // Собрать услуги для добавления
            //     CollectServicesForInsertion();

            //     // Запустить "OLP: Подпроцесс - Обновление услуг контакта v 3.0.1"
            //     IProcessEngine processEngine = _userConnection.ProcessEngine;
            //     IProcessExecutor processExecutor = processEngine.ProcessExecutor;

            //     try
            //     {
            //         processExecutor.Execute(
            //                 "_PROCESS",
            //                 new Dictionary<string, string> { {"_PARAMETR", _KEY} }
            //                 );
            //     }
            //     catch (Exception e)
            //     {

            //     }
            // }
            // return;
        }

        public bool GetOlpFirstStage()
        {
            try
            {
                string sql = $@"
                    SELECT 
                    BooleanValue 
                    FROM 
                    SysSettingsValue 
                    WHERE 
                    SysSettingsId = (
                            SELECT id FROM SysSettings WHERE Code LIKE 'OLPIsFirstStepToPROD'
                            )";

                CustomQuery query = new CustomQuery(_userConnection, sql);

                using (var db = _userConnection.EnsureDBConnection())
                {
                    using (var reader = query.ExecuteReader(db))
                    {
                        if (reader.Read())
                        {
                            var res = reader.GetColumnValue<bool>("BooleanValue");
                            _log.Info("GetOlp: " + res);
                            return res;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return false;
        }

        public Contact ReadContactFromCase(Guid contId)
        {
            _log.Info("ReadContactFromCase");

            var contact = new Contact(_userConnection);
            Dictionary<string, object> conditions = new Dictionary<string, object> {
                { nameof(Contact.Id), contId}, 
            };
            _log.Info(contId);
            if (contact.FetchFromDB(conditions))
            {
                _log.Info("Contact found");
                return contact;
            }
            _log.Info("Contact is not found");
            return null;
        }

        public void UpdateCaseToFirstLineSupport()
        {
            _log.Info("UpdateCaseToFirstLineSupport");
            string sql = $@"
                UPDATE " + 
                "\"Case\" \n" + 
                $@"SET 
                    OlpGroupServicesId = '{OLP_GENERAL_FIRST_LINE_SUPPORT}', 
                    OlpSupportLineId = '{OLP_OR_FIRST_LINE_SUPPORT}',
                    OlpImportantId = '{CASE_IMPORTANCY_NOT_IMPOTANT}',
                    OlpUrgencyId = '{CASE_URGENCY_TYPE_NOT_URGENT}',
                    CategoryId = '{caseCategory}'
                WHERE 
                    Id = '{caseId}'";
            try
            {
                CustomQuery query = new CustomQuery(_userConnection, sql);
                query.Execute();
                _log.Info(sql);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public Activity GetParentActivityFromCase()
        {
            try
            {
                _log.Info("GetParentActivityFromCase");
                var activity = new Activity(_userConnection);
                Dictionary<string, object> conditions = new Dictionary<string, object> {
                    { nameof(Activity.Id), parentActivityId},
                    { nameof(Activity.Type), ACTIVITY_TYPE_EMAIL } 
                };

                if (activity.FetchFromDB(conditions))
                {
                    _log.Info("Activity found");
                    return activity;
                }
                _log.Info("Activity NOT found");
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return null;
        }

        // Найти дежурную группу по email в кому/копия
        public Guid GetServiceGroupExtra()
        {

            _log.Info("GetServiceGroupExtra");
            try
            {
                string sql = $@"
                    SELECT * FROM OlpServiceGroup
                    WHERE Id IS NOT NULL AND
                    OlpSgEmailId IS NOT NULL AND
                    OlpTypeGroupServiceId = '{SERVICE_GROUP_TYPE_EXTRA}'";

                CustomQuery query = new CustomQuery(_userConnection, sql);

                using (var db = _userConnection.EnsureDBConnection())
                {
                    using (var reader = query.ExecuteReader(db))
                    {

                        Guid ExtraGroupIdTemp = System.Guid.Empty;

                        while (reader.Read())
                        {
                            /**LEGACY**/
                            string EmailBoxName = "";
                            string EmailBoxALias = "";

                            var ExtraGroupId = reader.GetColumnValue<Guid>("Id");

                            var ExtraEmailBox = reader.GetColumnValue<Guid>("OlpSgEmailId");

                            //ищем текстовое значение ящика по ГО
                            EntitySchemaQuery ExtraEmailBoxString = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "MailboxForIncidentRegistration");
                            ExtraEmailBoxString.PrimaryQueryColumn.IsAlwaysSelect = true;
                            ExtraEmailBoxString.ChunkSize = 1;
                            ExtraEmailBoxString.Filters.Add(ExtraEmailBoxString.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ExtraGroupEmailBoxId));
                            ExtraEmailBoxString.AddColumn("Name");
                            ExtraEmailBoxString.AddColumn("AliasAddress");

                            var MailboxSyncSettings = ExtraEmailBoxString.AddColumn("MailboxSyncSettings.Id"); //Ид. ящика из настройки почтовых ящиков
                            EntityCollection CollectionEmailText = ExtraEmailBoxString.GetEntityCollection(_userConnection);
                            //Найден ящик для регистрации обращений
                            if (CollectionEmailText.IsNotEmpty())
                            {
                                foreach (var itemsemail in CollectionEmailText)
                                {
                                    EmailBoxName = itemsemail.GetTypedColumnValue<string>("Name"); //название в справочнике ящиков для рег. обращений
                                    EmailBoxALias = itemsemail.GetTypedColumnValue<string>("AliasAddress");
                                    if( !string.IsNullOrEmpty(EmailBoxALias)){    
                                        string[] words = EmailBoxName.Split('(');
                                        EmailBoxName = words[0];}

                                    if (!string.IsNullOrEmpty(EmailBoxALias) && (email.ToUpper().Contains(EmailBoxName.ToUpper().Trim())
                                                || email.ToUpper().Contains(EmailBoxALias.ToUpper().Trim())
                                                || copies.ToUpper().Contains(EmailBoxALias.ToUpper().Trim())
                                                || copies.ToUpper().Contains(EmailBoxName.ToUpper().Trim())))
                                    {
                                        ExtraGroupIdByEmail = ExtraGroupId;
                                        ExtraGroupEmailBox = itemsemail.GetTypedColumnValue<string>("Name");
                                        ExtraGroupEmailBoxId = itemsemail.GetTypedColumnValue<Guid>(MailboxSyncSettings.Name);
                                        ExtraGroupIdTemp = ExtraGroupId;
                                    }
                                    else if(email.ToUpper().Contains(EmailBoxName.ToUpper().Trim())
                                            || copies.ToUpper().Contains(EmailBoxName.ToUpper().Trim()))
                                    {
                                        ExtraGroupIdByEmail = ExtraGroupId;
                                        ExtraGroupEmailBox = itemsemail.GetTypedColumnValue<string>("Name");
                                        ExtraGroupEmailBoxId = itemsemail.GetTypedColumnValue<Guid>(MailboxSyncSettings.Name);
                                        ExtraGroupIdTemp = ExtraGroupId;
                                    }
                                }
                            }

                            if (ExtraGroupIdTemp != Guid.Empty)
                            {

                                EntitySchemaQuery esq = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                                esq.PrimaryQueryColumn.IsAlwaysSelect = true;
                                esq.ChunkSize = 1;
                                var OlpTypeScheduleWorks = esq.AddColumn("OlpTypeScheduleWorks.Name");
                                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ExtraGroupIdTemp));
                                EntityCollection entityCollection = esq.GetEntityCollection(_userConnection);
                                //Идем в цикл если коллекция не пустая
                                if (entityCollection.IsNotEmpty()) 
                                {
                                    foreach (var groupsshedule in entityCollection) 
                                    {
                                        ExtraSheduleTypeByMail = groupsshedule.GetTypedColumnValue<string>(OlpTypeScheduleWorks.Name);
                                        return Guid.Empty;
                                    }
                                }
                            }
                        }
                    }
                }
                return Guid.Empty;
            }
            /**LEGACY**/
            catch (Exception e)
            {
                _log.Error("Exception at GetServiceGroupExtra: " + e);
            }
            return Guid.Empty;
        }

        // Найти основную группу по email в кому/копия
        public Guid GetServiceGroupMain()
        {
            _log.Info("GetServiceGroupMain");
            try
            {
                string sql = $@"
                    SELECT * FROM OlpServiceGroup
                    WHERE Id IS NOT NULL AND
                    OlpSgEmailId IS NOT NULL AND
                    OlpTypeGroupServiceId = '{SERVICE_GROUP_TYPE_MAIN}'"; 

                CustomQuery query = new CustomQuery(_userConnection, sql);

                using (var db = _userConnection.EnsureDBConnection())
                {
                    using (var reader = query.ExecuteReader(db))
                    {
                        Guid MainGroupIdTemp = Guid.Empty;

                        while (reader.Read())
                        {

                            /**LEGACY**/
                            string EmailBoxName = "";
                            string EmailBoxALias = "";

                            // Ид.ГО
                            Guid MainGroupId = reader.GetColumnValue<Guid>("Id");

                            // Ид.почтового ящика основной группы
                            Guid MainGroupEmailBoxId = reader.GetColumnValue<Guid>("OlpSgEmailId");

                            //ищем текстовое значение ящика по ГО
                            EntitySchemaQuery EsqEmailBoxString = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "MailboxForIncidentRegistration");
                            EsqEmailBoxString.PrimaryQueryColumn.IsAlwaysSelect = true;
                            EsqEmailBoxString.ChunkSize = 1;
                            EsqEmailBoxString.Filters.Add(EsqEmailBoxString.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", MainGroupEmailBoxId));
                            EsqEmailBoxString.AddColumn("Name");
                            EsqEmailBoxString.AddColumn("AliasAddress");

                            var MailboxSyncSettings= EsqEmailBoxString.AddColumn("MailboxSyncSettings.Id"); //Ид. ящика из настройки почтовых ящиков

                            EntityCollection CollectionEmailText = EsqEmailBoxString.GetEntityCollection(_userConnection);

                            //Найден ящик для регистрации обращений
                            if (CollectionEmailText.IsNotEmpty()){
                                foreach (var itemsemail in CollectionEmailText){
                                    EmailBoxName = itemsemail.GetTypedColumnValue<string>("Name"); //название в справочнике ящиков для рег. обращений
                                    EmailBoxALias = itemsemail.GetTypedColumnValue<string>("AliasAddress");
                                    //	var servicegroupid = itemsemail.GetTypedColumnValue<Guid>("Id");

                                    if( !string.IsNullOrEmpty(EmailBoxALias))
                                    {
                                        string[] words = EmailBoxName.Split('(');
                                        EmailBoxName = words[0];
                                    }

                                    if (!string.IsNullOrEmpty(EmailBoxALias) && (email.ToUpper().Contains(EmailBoxName.ToUpper().Trim())
                                                || email.ToUpper().Contains(EmailBoxALias.ToUpper().Trim())
                                                || copies.ToUpper().Contains(EmailBoxALias.ToUpper().Trim())
                                                || copies.ToUpper().Contains(EmailBoxName.ToUpper().Trim())))
                                    {
                                        MainGroupIdByEmail = MainGroupId;
                                        MainGroupEmailBox = itemsemail.GetTypedColumnValue<string>("Name");
                                        MainGroupEmailBoxId = itemsemail.GetTypedColumnValue<Guid>(MailboxSyncSettings.Name);
                                        MainEmailBoxIdForReg = itemsemail.GetTypedColumnValue<Guid>("Id");
                                        MainGroupIdTemp = MainGroupId;
                                    }
                                    else if(email.ToUpper().Contains(EmailBoxName.ToUpper().Trim())
                                            || copies.ToUpper().Contains(EmailBoxName.ToUpper().Trim()))
                                    {
                                        MainGroupIdByEmail = MainGroupId;
                                        MainGroupEmailBox = itemsemail.GetTypedColumnValue<string>("Name");
                                        MainGroupEmailBoxId = itemsemail.GetTypedColumnValue<Guid>(MailboxSyncSettings.Name);
                                        MainEmailBoxIdForReg = itemsemail.GetTypedColumnValue<Guid>("Id");
                                        MainGroupIdTemp = MainGroupId;
                                    }
                                }
                            }

                            if (MainGroupIdTemp != Guid.Empty)
                            {
                                EntitySchemaQuery esq = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                                esq.PrimaryQueryColumn.IsAlwaysSelect = true;
                                esq.ChunkSize = 1;
                                var OlpTypeScheduleWorks = esq.AddColumn("OlpTypeScheduleWorks.Name");

                                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", MainGroupIdTemp));

                                EntityCollection entityCollection = esq.GetEntityCollection(_userConnection);

                                //Идем в цикл если коллекция не пустая
                                if (entityCollection.IsNotEmpty()) 
                                {
                                    foreach (var groupsshedule in entityCollection) 
                                    {
                                        MainSheduleTypeByMail = groupsshedule.GetTypedColumnValue<string>(OlpTypeScheduleWorks.Name);
                                        break;
                                    }
                                }
                            }
                            return MainGroupId; // TODO
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error("Exception at GetServiceGroupMain: " + e);
            }
            return Guid.Empty;
            /**LEGACY**/
        }

        public void SetTravelParameter()
        {

            _log.Info("SetTravelParameter");

            try
            {
                var theme = activity.GetTypedColumnValue<string>("Title");
                var body = activity.GetTypedColumnValue<string>("Body");

                _log.Info("theme: " + theme + "; body: " + body);

                var themetravel = "";
                if (!string.IsNullOrEmpty(theme) && theme.ToUpper().Contains("TRAVEL-"))
                {

                    var regex = new Regex(@"(?<=TRAVEL-)\d+");

                    foreach (Match match in regex.Matches(theme))
                    {
                        themetravel = match.Value.ToString();
                        if(!string.IsNullOrEmpty(themetravel))
                        {
                            themetravel = "TRAVEL-" + themetravel;
                            break;
                        }
                    }
                }


                if (string.IsNullOrEmpty(themetravel) && !string.IsNullOrEmpty(body) && body.ToUpper().Contains("TRAVEL-"))
                {

                    var regex = new Regex(@"(?<=TRAVEL-)\d+");

                    foreach (Match match in regex.Matches(body))
                    {
                        themetravel = match.Value.ToString();
                        if(!string.IsNullOrEmpty(themetravel))
                        {
                            themetravel = "TRAVEL-" + themetravel;
                            break;
                        }
                    }

                }
                 
                if (!string.IsNullOrEmpty(themetravel))
                {
                    string sql1 = $@"
                        UPDATE " + 
                        "\"Case\" \n" + 
                        $@"SET 
                        OlpTRAVELNumber = '{themetravel}'
                        WHERE 
                        id = '{caseId}'";

                    _log.Info("SQL1: " + sql1);
                    CustomQuery query1 = new CustomQuery(_userConnection, sql1);
                    query1.Execute();
                    
                    string sql2 = $@"
                        UPDATE " + 
                        "\"Case\" \n" + 
                        $@"SET 
                        OlpTRAVELNumber = '{themetravel}_Закрыто/Отмененно'
                        WHERE 
                        statusId = '{CASE_STATUS_CLOSED}' OR statusId = '{CASE_STATUS_CANCELED}'";

                    _log.Info("SQL2: " + sql2);
                    CustomQuery query2 = new CustomQuery(_userConnection, sql2);
                    query2.Execute();
                }

                
            }
            catch (Exception e)
            {
                _log.Error("Exception at SetTravelParameter: " + e);
            }

        }

        public void SetSiburParameter()
        {
            _log.Info("SetSiburParameter");
            try
            {
                var theme = activity.GetTypedColumnValue<string>("Title");
                var body = activity.GetTypedColumnValue<string>("Body");

                _log.Info("theme: " + theme + "; body: " + body);

                var themetravel = "";

                if(string.IsNullOrEmpty(themetravel) && !string.IsNullOrEmpty(theme) && theme.ToUpper().Contains("ЗАЯВКА ПО РЕЛОКАЦИИ_")) 
                {

                    var regexurgent = new Regex(@"(?<=Заявка по релокации_)\[(.+)\]");
                    foreach (Match match in regexurgent.Matches(theme))
                    {
                        themetravel = match.Value.ToString();
                        if(!string.IsNullOrEmpty(themetravel))
                        {
                            themetravel = "Заявка по релокации_" + themetravel;
                            break;
                        }
                    }
                }


                if (!string.IsNullOrEmpty(themetravel))
                {
                    string sql1 = $@"
                        UPDATE " + 
                        "\"Case\" \n" + 
                        $@"SET 
                        OlpReloThemeSibur = '{themetravel}'
                        WHERE 
                        id = '{caseId}'";

                    CustomQuery query1 = new CustomQuery(_userConnection, sql1);
                    query1.Execute();
                    _log.Info("SQL1: " + sql1);
                
                    string sql2 = $@"
                        UPDATE " + 
                        "\"Case\" \n" + 
                        $@"SET 
                        OlpReloThemeSibur = '{themetravel}_Закрыто/Отмененно'
                        WHERE 
                        statusId = '{CASE_STATUS_CLOSED}' OR statusId = '{CASE_STATUS_CANCELED}'";


                    _log.Info("SQL2: " + sql2);
                    CustomQuery query2 = new CustomQuery(_userConnection, sql2);
                    query2.Execute();
                }

            }
            catch (Exception e)
            {
                _log.Error("Exception at SetSiburParameter: " + e);
            }

        }

        public string GetDomainFromEmail()
        {
            string s = email;
			string d = email;
			
            string[] words = s.Split('<');
			string[] words1 = words[1].Split('>');
			
            email = words1[0]; // TODO
			
			string[] wordd = d.Split('@');
			
            return wordd[1];
        }

        public Guid GetAccountIdFromAccountCommunication()
        {
            _log.Info("GetAccountIdFromAccountCommunication");
            
            var domain = GetDomainFromEmail();

            string sql = $@"
                SELECT TOP 1 * FROM AccountCommunication
                WHERE 
                (
                 CommunicationType = '{COMMUNICATION_TYPE_EMAIL_DOMAIN}'
                 AND 
                 Number = '{domain}' 
                )
                OR
                (
                 CommunicationType = 'Email'
                 AND 
                 Number = '{email}' 
                )";

            CustomQuery query = new CustomQuery(_userConnection, sql);

            using (var db = _userConnection.EnsureDBConnection())
            {
                using (var reader = query.ExecuteReader(db))
                {
                    if (reader.Read())
                    {
                        return reader.GetColumnValue<Guid>("Account");
                    }
                }
            }
            return Guid.Empty;
        }

        public Account FetchAccountById(Guid accountId)
        {
            _log.Info("FetchAccountById");

            var account = new Account(_userConnection);
            Dictionary<string, object> conditions = new Dictionary<string, object> {
                { nameof(Account.Id), accountId },
            };

            if (account.FetchFromDB(conditions))
            {
                return account;
            }
            return null;
        }

        public Account FetchAccountByEis(Guid accountId)
        {
            _log.Info("FetchAccountByEis");
            var account = new Account(_userConnection);
            Dictionary<string, object> conditions = new Dictionary<string, object> {
                { nameof(Account.OlpCode), accountId },
            };

            if (account.FetchFromDB(conditions))
                return account;
            return null;
        }

        public void SetContactType(Guid contactId, Guid type)
        {
            _log.Info("SetContactType");
            var contactIdTemp = contact.GetTypedColumnValue<Guid>("Id");
            var companyIdTemp = account.GetTypedColumnValue<Guid>("Id");

            string sql = $@"
                UPDATE 
                    Contact
                SET 
                    Type = '{type}',
                    Account = '{companyIdTemp}'
                WHERE id = '{contactIdTemp}'";

            CustomQuery query = new CustomQuery(_userConnection, sql);
            query.Execute();
        }

        public void SetSpamOnCase()
        {
            _log.Info("SetSpamOnCase");
            var contactId = contact.GetTypedColumnValue<Guid>("Id");
            string sql = $@"
                UPDATE 
                    Contact
                SET 
                    Type = '{CONTACT_TYPE_UNDEFINED_CLIENT_SPAM}', 
                    Account = '{OLP_AC_COMPANY_IS_NOT_DEFINED}' 
                WHERE
                    Id = '{contactId}'";

            CustomQuery query = new CustomQuery(_userConnection, sql);
            query.Execute();
        }

        public void RefreshContact()
        {
            _log.Info("RefreshContact");
            
            string OlpLnFnPat = eis.FirstName.English + " " + eis.MiddleName.English + " " + eis.LastName.English;
            var accountId = account.GetTypedColumnValue<Guid>("Id");
            
            string sql = $@"
                UPDATE
                    Contact
                SET
                    Email = '{email}', 
                    Account = '{accountId}',
                    OlpBooleanAeroclubCheck = 1,
                    OlpSignVip = '{eis.IsVip}',
                    OlpContactProfileConsLink = '{eis.ProfileLink}',
                    OlpLnFnPat = '{OlpLnFnPat}',
                    GivenName = '{eis.FirstName.Russian}',
                    MiddleName = '{eis.MiddleName.Russian}',
                    Surname = '{eis.LastName.Russian}',
                    OlpSignVipPlatf = '{eis.IsVipOnPlatform}',
                    OlpIsAuthorizedPerson = '{eis.IsAuthorized}',
                    OlpIsContactPerson = '{eis.IsContactPerson}',
                    Type = '{CONTACT_TYPE_CLIENT}',
                    OlpExternalContId = '{eis.Id}' 
                WHERE 
                    Id = '{contactId}'";

            CustomQuery query = new CustomQuery(_userConnection, sql);
            query.Execute();
        }

        public void RefreshContactCompanyAndEmail()
        {
            _log.Info("RefreshContactCompanyAndEmail");
            var contactId = contact.GetTypedColumnValue<Guid>("Id");
            var accountId = account.GetTypedColumnValue<Guid>("Id");
            string sql = $@"
                UPDATE 
                    Contact
                SET,
                    Email = '{email}',
                    Account = '{accountId}',
                    Type = '{CONTACT_TYPE_CLIENT}',
                WHERE 
                    Id = '{contactId}'";

            CustomQuery query = new CustomQuery(_userConnection, sql);
            query.Execute();
        }

        public void RefreshContactTypeAndEmail()
        {
            _log.Info("RefreshContactTypeAndEmail");

            var contactId = contact.GetTypedColumnValue<Guid>("Id");
            string sql = $@"
                UPDATE 
                    Contact
                SET
                    Email = '{email}', 
                    Type = '{CONTACT_TYPE_CLIENT}',
                WHERE 
                    Id = '{contactId}'";

            CustomQuery query = new CustomQuery(_userConnection, sql);
            query.Execute();
        }

        public Guid GetHoldingFromAccountBindedToEis()
        {
            _log.Info("GetHoldingFromAccountBindedToEis");
            
            // var contactId = contact.GetTypedColumnValue<Guid>("Id");

            string sql = $@"
                SELECT OlpHoldingId FROM Contact c 
                INNER JOIN Account a ON a.Id = c.AccountId
                WHERE c.Id = '{contactId}'";
            _log.Info(sql);
            CustomQuery query = new CustomQuery(_userConnection, sql);

            using (var db = _userConnection.EnsureDBConnection())
            {
                using (var reader = query.ExecuteReader(db))
                {
                    if (reader.Read())
                    {
                        _log.Info("Holding found: " + reader.GetColumnValue<Guid>("OlpHoldingId"));
                        return reader.GetColumnValue<Guid>("OlpHoldingId");
                    }
                }
            }
            _log.Info("Holding NOT found");
            return Guid.Empty;
        }

        public void GetContactAfterRefreshing(Guid contactId)
        {
            _log.Info("GetContactAfterRefreshing");
            var updatedContact = new Contact(_userConnection);
            Dictionary<string, object> conditions = new Dictionary<string, object> {
                { nameof(Contact.Id), contactId}, 
            };

            if (updatedContact.FetchFromDB(conditions))
            {
                contact =  updatedContact;
            }
        }

        public void AddAccountToEmail()
        {
            try
            {
                _log.Info("AddAccountToEmail");
                string sql = $@"
                    UPDATE 
                        Activity
                    SET 
                        AccountId = '{clientCompanyId}' 
                    WHERE 
                        Id = '{parentActivityId}'";
                _log.Info(sql);
                CustomQuery query = new CustomQuery(_userConnection, sql);
                query.Execute();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public OlpServiceGroup GetServiceGroupBySelectedId()
        {
            try
            {
                _log.Info("GetServiceGroupBySelectedId");
                var serviceGroup = new OlpServiceGroup(_userConnection);
                Dictionary<string, object> conditions = new Dictionary<string, object> {
                    { nameof(Account.Id), selectedServiceGroupId},
                };
                _log.Info("selectedServiceGroupId: " + selectedServiceGroupId);
                if (serviceGroup.FetchFromDB(conditions))
                {
                    return serviceGroup;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return null;
        }

        // Найти основную ГО для контакта по компаниям и ВИП Платформа
        public void GetMainServiceGroup()
        {
            _log.Info("GetMainServiceGroup");
            /**LEGACY**/

            //Считать признак поиска в ЕИС
            //Считать Ид. компании 
            Guid companyid = clientCompanyId;
            //Считать Ид. холдинга
            Guid holdingid = holding;
            //Считать ид. ящика из ГО по почте
            Guid MainEmailBoxIdForRegId = MainEmailBoxIdForReg;
            //Считать ВИП Платформа
            bool isvipplatform = clientVipPlatform;

            EntitySchemaQuery esq = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            esq.PrimaryQueryColumn.IsAlwaysSelect = true;	

            esq.AddColumn("[OlpGroupServiceAccount:OlpServiceGroupDetail:Id].OlpAccount"); //Ид.компании-холдинга из детали
            var OlpTypeServiceGroup = esq.AddColumn("OlpTypeGroupService.Name");
            var OlpTypeScheduleWorks = esq.AddColumn("OlpTypeScheduleWorks.Name");
            var orFilterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);

            //Считать данные раздела ГО по основным группам/ВИП платформа
            if (isvipplatform == true)
            {
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "OlpTypeGroupService.Name", "ВИП Платформа"));	
            }
            else
            {
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "OlpTypeGroupService.Name", "Основная группа"));	

                if(MainEmailBoxIdForRegId != Guid.Empty)
                {
                    esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "OlpSgEmail.Id", MainEmailBoxIdForRegId));
                }

                // Компания || Холдинг
                orFilterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpGroupServiceAccount:OlpServiceGroupDetail:Id].OlpAccount", companyid));
                orFilterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpGroupServiceAccount:OlpServiceGroupDetail:Id].OlpAccount", holdingid));
                esq.Filters.Add(orFilterGroup);
            }  

            var list = new List<ServiceGroupElement>();
            EntityCollection entityCollection = esq.GetEntityCollection(_userConnection);

            //Идем в цикл если коллекция не пустая
            if (entityCollection.IsNotEmpty()) 
            {
                foreach (var servicegroups in entityCollection) 
                {

                    var item = new ServiceGroupElement();
                    var servicegroupid = servicegroups.GetTypedColumnValue<Guid>("Id");
                    var servicegroupttid = servicegroups.GetTypedColumnValue<string>(OlpTypeScheduleWorks.Name);
                    var servicegrouptype = servicegroups.GetTypedColumnValue<string>(OlpTypeServiceGroup.Name);

                    item._serviceGroupId = servicegroupid;
                    item._timeTableId = servicegroupttid;
                    ServiceGroupForOrder = servicegroupid;

                    list.Add(item);
                }

                ProcessSchemaParameterServiceGroupCollection = list; 
                StringBuilder sb = new StringBuilder();
                foreach (var item in list)
                {
                    sb.Append("Item: " + item._serviceGroupId + "; " + item._timeTableId);
                }
                _log.Info(sb.ToString());
            }        
            else
            { 
                _log.Info("ProcessSchemaParameterIsDutyGroup = true");
                //Дежурная группа
                ProcessSchemaParameterIsDutyGroup = true;
                isExtraServiceGroup = true; // TODO
            }

            /**LEGACY**/
        }

        // Найти ГО основную по графику работы
        public void GetMainServiceGroupBasedOnTimetable()
        {
            _log.Info("GetMainServiceGroupBasedOnTimetable");
            /**LEGACY**/

            DateTime CurrentDayTime = DateTime.UtcNow.AddHours(3);
            var servicegrouplist = ProcessSchemaParameterServiceGroupCollection;
            var ServiceGroupIdTemp = Guid.Empty;

            foreach (var item in servicegrouplist)
            {
                Guid ServiceGroupId = item._serviceGroupId;
                string ServiceTimeTableId = item._timeTableId;
                _log.Info("SGI: " + ServiceGroupId + "; ServiceTimeTableId: " + ServiceTimeTableId);
                if (ServiceTimeTableId == "Круглосуточно")
                {
                    ServiceGroupIdTemp = ServiceGroupId;
                    break;
                }

                //Приоритетная проверка по Праздничным-выходным дням
                EntitySchemaQuery EsqHoliday = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                EsqHoliday.PrimaryQueryColumn.IsAlwaysSelect = true;
                EsqHoliday.ChunkSize = 1;
                EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));
                // Найти График в праздничные дни
                EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", CurrentDayTime));
                EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", CurrentDayTime));
                EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Выходной"));


                EntityCollection CollectionHoliday = EsqHoliday.GetEntityCollection(_userConnection);
                //Есть выходной-праздничный день перейти к следующей ГО
                if (CollectionHoliday.IsNotEmpty())
                {
                    continue;
                }

                //Приоритетная проверка по Праздничным-рабочим дням
                EntitySchemaQuery EsqHolidayWork = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                EsqHolidayWork.PrimaryQueryColumn.IsAlwaysSelect = true;
                EsqHolidayWork.ChunkSize = 1;
                // Найти График в праздничные дни
                EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", CurrentDayTime));
                EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", CurrentDayTime));
                EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
                EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

                EntityCollection CollectionHolidayWork = EsqHolidayWork.GetEntityCollection(_userConnection);
                
                //Есть рабочий-праздничный день записать ГО и выйти из цикла
                if (CollectionHolidayWork.IsNotEmpty())
                {
                    foreach (var itemholidaywork in CollectionHolidayWork) 
                    {
                        ServiceGroupIdTemp = itemholidaywork.GetTypedColumnValue<Guid>("Id");
                    }
                    
                    if (ServiceGroupIdTemp != Guid.Empty)
                    {
                        break;
                    }
                }

                //Приоритетная проверка по Праздничным-рабочим дням - есть ли они на текущую дату?
                EntitySchemaQuery EsqHolidayWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                EsqHolidayWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
                EsqHolidayWorkDate.ChunkSize = 1;
                EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow.AddHours(3).Date));
                EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Less, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow.AddHours(3).Date.AddDays(1)));
                EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
                EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

                EntityCollection CollectionHolidayWorkDate = EsqHolidayWorkDate.GetEntityCollection(_userConnection);
                //Есть рабочий-праздничный день записать ГО и выйти из цикла
                if (CollectionHolidayWorkDate.IsNotEmpty())
                {
                    continue;
                }

                //можно смотреть по стандартному графику
                EntitySchemaQuery EsqStandartWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                EsqStandartWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
                EsqStandartWorkDate.ChunkSize = 2;

                var WeekDayColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code");
                var TimeFromColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeFrom");
                var TimeToColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeTo");
                var orFilterGroup = new EntitySchemaQueryFilterCollection(EsqStandartWorkDate, LogicalOperationStrict.Or);

                EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTypeDay.Name", "Рабочий")); // тип рабочего дня выходной/рабочий
                EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

                orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())); // день недели день недели -1
                orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.AddDays(-1).DayOfWeek.ToString())); // день недели день недели -1
                EsqStandartWorkDate.Filters.Add(orFilterGroup);

                EntityCollection CollectionStandartWorkDate = EsqStandartWorkDate.GetEntityCollection(_userConnection);

                if (CollectionStandartWorkDate.IsNotEmpty()) 
                {
                    foreach (var itemstandartwork in CollectionStandartWorkDate)
                    {

                        var WeekDay  = itemstandartwork.GetTypedColumnValue<string>(WeekDayColumn.Name);
                        var TimeFrom = itemstandartwork.GetTypedColumnValue<DateTime>(TimeFromColumn.Name);
                        var TimeTo   = itemstandartwork.GetTypedColumnValue<DateTime>(TimeToColumn.Name);

                        if (TimeFrom >= TimeTo) 
                        {
                            TimeTo = TimeTo.AddDays(1);
                        }
                        
                        if (WeekDay != DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())
                        {
                            TimeFrom = TimeFrom.AddDays(-1);TimeTo = TimeTo.AddDays(-1);
                        }

                        // Подходит ли основной график ГО
                        if (CurrentDayTime>TimeFrom && CurrentDayTime < TimeTo)
                        {
                            ServiceGroupIdTemp = itemstandartwork.GetTypedColumnValue<Guid>("Id");
                            break;
                        }
                    }
                    
                    if (ServiceGroupIdTemp != Guid.Empty)
                    {
                        break;
                    }
                }
            }

            if (ServiceGroupIdTemp == Guid.Empty){
                //дежурная группа 
                ProcessSchemaParameterIsDutyGroup = true;
            }
            else 
            {
                ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                selectedServiceGroupId = ServiceGroupIdTemp;
            }

            return;
            
            /**LEGACY**/
        }

        // Найти ГО  основную по графику работы 1 этап
        public void GetMainServiceGroupBasedOnTimetableOlpFirstStage()
        {
            /**LEGACY**/
            _log.Info("GetMainServiceGroupBasedOnTimetableOlpFirstStage");

            try
            {
                _log.Info("is collection null: " + (ProcessSchemaParameterServiceGroupCollection == null));
                DateTime CurrentDayTime = DateTime.UtcNow.AddHours(3);
                var servicegrouplist = ProcessSchemaParameterServiceGroupCollection;
                var ServiceGroupIdTemp = Guid.Empty;


                foreach (var item in servicegrouplist) 
                {
                    string ServiceTimeTableId = item._timeTableId;
                    Guid ServiceGroupId = item._serviceGroupId;

                    _log.Info("ServiceGroupId: " + ServiceGroupId + "; ServiceTimeTableId: " + ServiceTimeTableId);
                    if (ServiceTimeTableId == "Круглосуточно")
                    {
                        ServiceGroupIdTemp = ServiceGroupId;
                        break;
                    }

                    //Приоритетная проверка по Праздничным-выходным дням
                    EntitySchemaQuery EsqHoliday = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                    EsqHoliday.PrimaryQueryColumn.IsAlwaysSelect = true;
                    EsqHoliday.ChunkSize = 1;
                    EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));
                    // Найти График в праздничные дни
                    EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow));
                    EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow));
                    EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Выходной"));


                    EntityCollection CollectionHoliday = EsqHoliday.GetEntityCollection(_userConnection);
                    //Есть выходной-праздничный день перейти к следующей ГО
                    if (CollectionHoliday.IsNotEmpty())
                    {
                        continue;
                    }

                    //Приоритетная проверка по Праздничным-рабочим дням
                    EntitySchemaQuery EsqHolidayWork = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                    EsqHolidayWork.PrimaryQueryColumn.IsAlwaysSelect = true;
                    EsqHolidayWork.ChunkSize = 1;
                    // Найти График в праздничные дни
                    EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow));
                    EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow));
                    EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
                    EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

                    EntityCollection CollectionHolidayWork = EsqHolidayWork.GetEntityCollection(_userConnection);
                    //Есть рабочий-праздничный день записать ГО и выйти из цикла
                    if (CollectionHolidayWork.IsNotEmpty())
                    {
                        foreach (var itemholidaywork in CollectionHolidayWork) 
                        {
                            ServiceGroupIdTemp = itemholidaywork.GetTypedColumnValue<Guid>("Id");
                        }

                        if (ServiceGroupIdTemp != Guid.Empty)
                        {
                            break;
                        }
                    }

                    //Приоритетная проверка по Праздничным-рабочим дням - есть ли они на текущую дату?
                    EntitySchemaQuery EsqHolidayWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                    EsqHolidayWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
                    EsqHolidayWorkDate.ChunkSize = 1;
                    EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow.Date));
                    EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Less, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow.Date.AddDays(1)));
                    EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
                    EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

                    EntityCollection CollectionHolidayWorkDate = EsqHolidayWorkDate.GetEntityCollection(_userConnection);
                    //Есть рабочий-праздничный день записать ГО и выйти из цикла
                    if (CollectionHolidayWorkDate.IsNotEmpty())
                    {
                        continue;
                    }

                    //можно смотреть по стандартному графику
                    EntitySchemaQuery EsqStandartWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                    EsqStandartWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
                    EsqStandartWorkDate.ChunkSize = 2;

                    var WeekDayColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code");
                    var TimeFromColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeFrom");
                    var TimeToColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeTo");
                    var orFilterGroup = new EntitySchemaQueryFilterCollection(EsqStandartWorkDate, LogicalOperationStrict.Or);

                    EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTypeDay.Name", "Рабочий")); // тип рабочего дня выходной/рабочий
                    EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

                    orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())); // день недели день недели -1
                    orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.AddDays(-1).DayOfWeek.ToString())); // день недели день недели -1
                    EsqStandartWorkDate.Filters.Add(orFilterGroup);

                    EntityCollection CollectionStandartWorkDate = EsqStandartWorkDate.GetEntityCollection(_userConnection);

                    if (CollectionStandartWorkDate.IsNotEmpty()) 
                    {
                        foreach (var itemstandartwork in CollectionStandartWorkDate)
                        {

                            var WeekDay  = itemstandartwork.GetTypedColumnValue<string>(WeekDayColumn.Name);
                            var TimeFrom = itemstandartwork.GetTypedColumnValue<DateTime>(TimeFromColumn.Name);
                            var TimeTo   = itemstandartwork.GetTypedColumnValue<DateTime>(TimeToColumn.Name);

                            if (TimeFrom >= TimeTo) {TimeTo = TimeTo.AddDays(1);}
                            if (WeekDay != DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString()){TimeFrom = TimeFrom.AddDays(-1);TimeTo = TimeTo.AddDays(-1);}

                            // Подходит ли основной график ГО
                            if (CurrentDayTime>TimeFrom && CurrentDayTime < TimeTo)
                            {
                                ServiceGroupIdTemp = itemstandartwork.GetTypedColumnValue<Guid>("Id");
                                break;
                            }
                        }
                        if (ServiceGroupIdTemp != Guid.Empty)
                        {
                            break;
                        }
                    }
                }
                _log.Info("ServiceGroupIdTemp == Guid.Empty: " + (ServiceGroupIdTemp == Guid.Empty));
                if (ServiceGroupIdTemp == Guid.Empty)
                {
                    //дежурная группа 
                    ProcessSchemaParameterIsDutyGroup = true;
                    isExtraServiceGroup = true; // TODO
                }
                else 
                {
                    ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                    selectedServiceGroupId = ServiceGroupIdTemp;
                    ProcessSchemaParameterIsDutyGroup = false;
                    _log.Info("selectedServiceGroupId: " + selectedServiceGroupId);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
            return;
         
            /**LEGACY**/
        }

        // Найти осн из почты по графику
        public void GetMainServiceGroupFromMainBasedOnTimeTable()
        {
            _log.Info("GetMainServiceGroupFromMainBasedOnTimeTable");
            DateTime CurrentDayTime = DateTime.UtcNow.AddHours(3);

            Guid ServiceGroupId = MainGroupIdByEmail;
            string sheduletype = MainSheduleTypeByMail;

            Guid ServiceGroupIdTemp = System.Guid.Empty;

            //можно смотреть по стандартному графику
            EntitySchemaQuery EsqStdWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqStdWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqStdWorkDate.ChunkSize = 1;

            var StdTimeFromColumn = EsqStdWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeFrom");
            var StdTimeToColumn = EsqStdWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeTo");

            EsqStdWorkDate.Filters.Add(EsqStdWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTypeDay.Name", "Рабочий")); // тип рабочего дня выходной/рабочий
            EsqStdWorkDate.Filters.Add(EsqStdWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", "Monday"));
            EsqStdWorkDate.Filters.Add(EsqStdWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            EntityCollection CollectionStdWorkDate = EsqStdWorkDate.GetEntityCollection(_userConnection);

            if (CollectionStdWorkDate.IsNotEmpty())
            {
                ProcessSchemaParameter2 = 1;
                foreach (var itemstdwork in CollectionStdWorkDate)
                {

                    var StdTimeFrom = itemstdwork.GetTypedColumnValue<DateTime>(StdTimeFromColumn.Name);
                    var StdTimeTo   = itemstdwork.GetTypedColumnValue<DateTime>(StdTimeToColumn.Name);
                    ProcessSchemaParameter2 = 1;

                    if(StdTimeFrom != null && StdTimeTo  != null)
                    {
                        DateFromForReply = StdTimeFrom.ToString("HH:mm");
                        DateToForReply = StdTimeTo.ToString("HH:mm");
                    }
                    else
                    {
                        DateFromForReply = "09:00";
                        DateToForReply = "19:00";
                    }

                    break;
                }
            }
            else
            {
                DateFromForReply = "09:00";
                DateToForReply = "19:00";
            }


            if (sheduletype == "Круглосуточно")
            {
                ServiceGroupIdTemp = ServiceGroupId;
                ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                selectedServiceGroupId = ServiceGroupIdTemp;
                ProcessSchemaParameterIsDutyGroup = false;
                return;
            }

            //Приоритетная проверка по Праздничным-выходным дням
            EntitySchemaQuery EsqHoliday = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHoliday.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHoliday.ChunkSize = 1;
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));
            // Найти График в праздничные дни
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow));
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow));
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Выходной"));

            EntityCollection CollectionHoliday = EsqHoliday.GetEntityCollection(_userConnection);
            //Есть выходной-праздничный день перейти к следующей ГО
            if (CollectionHoliday.IsNotEmpty())
            {
                ProcessSchemaParamServiceGroupId = ServiceGroupId;
                selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                ProcessSchemaParameterIsDutyGroup = true;
                ServiceGroupForOrder = ServiceGroupId;
                return ;
            }

            //Приоритетная проверка по Праздничным-рабочим дням
            EntitySchemaQuery EsqHolidayWork = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHolidayWork.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHolidayWork.ChunkSize = 1;
            // Найти График в праздничные дни
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            EntityCollection CollectionHolidayWork = EsqHolidayWork.GetEntityCollection(_userConnection);
            //Есть рабочий-праздничный день записать ГО и выйти из цикла
            if (CollectionHolidayWork.IsNotEmpty())
            {
                foreach (var itemholidaywork in CollectionHolidayWork) 
                {
                    ServiceGroupIdTemp = itemholidaywork.GetTypedColumnValue<Guid>("Id");
                }

                if (ServiceGroupIdTemp != Guid.Empty)
                {
                    ProcessSchemaParamServiceGroupId =  ServiceGroupIdTemp;
                    selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                    ProcessSchemaParameterIsDutyGroup  = false;
                    return;
                }
            }

            //Приоритетная проверка по Праздничным-рабочим дням - есть ли они на текущую дату?
            EntitySchemaQuery EsqHolidayWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHolidayWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHolidayWorkDate.ChunkSize = 1;
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow.Date));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Less, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow.Date.AddDays(1)));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            EntityCollection CollectionHolidayWorkDate = EsqHolidayWorkDate.GetEntityCollection(_userConnection);
            //Есть рабочий-праздничный день записать ГО и выйти из цикла
            if (CollectionHolidayWorkDate.IsNotEmpty())
            {
                ProcessSchemaParamServiceGroupId = ServiceGroupId;
                selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                ProcessSchemaParameterIsDutyGroup = true;
                ServiceGroupForOrder = ServiceGroupId;
                return;
            }

            //можно смотреть по стандартному графику
            EntitySchemaQuery EsqStandartWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqStandartWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqStandartWorkDate.ChunkSize = 2;

            var WeekDayColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code");
            var TimeFromColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeFrom");
            var TimeToColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeTo");
            var orFilterGroup = new EntitySchemaQueryFilterCollection(EsqStandartWorkDate, LogicalOperationStrict.Or);

            EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTypeDay.Name", "Рабочий")); // тип рабочего дня выходной/рабочий
            EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())); // день недели день недели -1
            orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.AddDays(-1).DayOfWeek.ToString())); // день недели день недели -1
            EsqStandartWorkDate.Filters.Add(orFilterGroup);

            EntityCollection CollectionStandartWorkDate = EsqStandartWorkDate.GetEntityCollection(_userConnection);

            if (CollectionStandartWorkDate.IsNotEmpty()) 
            {
                foreach (var itemstandartwork in CollectionStandartWorkDate)
                {

                    var WeekDay  = itemstandartwork.GetTypedColumnValue<string>(WeekDayColumn.Name);
                    var TimeFrom = itemstandartwork.GetTypedColumnValue<DateTime>(TimeFromColumn.Name);
                    var TimeTo   = itemstandartwork.GetTypedColumnValue<DateTime>(TimeToColumn.Name);

                    if (TimeFrom >= TimeTo) 
                    {
                        TimeTo = TimeTo.AddDays(1);
                    }

                    if (WeekDay != DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())
                    {
                        TimeFrom = TimeFrom.AddDays(-1);TimeTo = TimeTo.AddDays(-1);
                    }

                    // Подходит ли основной график ГО
                    if (CurrentDayTime>TimeFrom && CurrentDayTime < TimeTo)
                    {
                        ServiceGroupIdTemp = itemstandartwork.GetTypedColumnValue<Guid>("Id");
                        break;
                    }
                }
                
                if (ServiceGroupIdTemp != Guid.Empty)
                {
                    ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                    selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                    ProcessSchemaParameterIsDutyGroup = false;
                }
            }


            if (ServiceGroupIdTemp == Guid.Empty)
            {
                //дежурная группа
                ProcessSchemaParamServiceGroupId = ServiceGroupId;
                selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                ProcessSchemaParameterIsDutyGroup = true;
                ServiceGroupForOrder = ServiceGroupId;
            }
            /**LEGACY**/
        }

        // Найти ГО дежурную из кому/копии по графику работы
        public void GetExtraServiceGroupFromFromAndCopyBaseOnTimetable()
        {
            _log.Info("GetExtraServiceGroupFromFromAndCopyBaseOnTimetable");
            /**LEGACY**/
            
            DateTime CurrentDayTime = DateTime.UtcNow.AddHours(3);
            var k=0;			
            Guid ServiceGroupId = ExtraGroupIdByEmail;
            string sheduletype = ExtraSheduleTypeByMail;
            Guid ServiceGroupIdTemp = System.Guid.Empty;

            if (sheduletype == "Круглосуточно")
            {
                ServiceGroupIdTemp = ServiceGroupId;
                ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                ProcessSchemaParameterIsDutyGroup = false;
                return;
            }

            //Приоритетная проверка по Праздничным-выходным дням
            EntitySchemaQuery EsqHoliday = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHoliday.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHoliday.ChunkSize = 1;
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));
            // Найти График в праздничные дни
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow));
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow));
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Выходной"));


            EntityCollection CollectionHoliday = EsqHoliday.GetEntityCollection(_userConnection);
            //Есть выходной-праздничный день перейти к следующей ГО
            if (CollectionHoliday.IsNotEmpty())
            {
                ProcessSchemaParameterIsDutyGroup = true;
                return;
            }

            //Приоритетная проверка по Праздничным-рабочим дням
            EntitySchemaQuery EsqHolidayWork = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHolidayWork.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHolidayWork.ChunkSize = 1;
            // Найти График в праздничные дни
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            EntityCollection CollectionHolidayWork = EsqHolidayWork.GetEntityCollection(_userConnection);
            //Есть рабочий-праздничный день записать ГО и выйти из цикла
            if (CollectionHolidayWork.IsNotEmpty())
            {
                foreach (var itemholidaywork in CollectionHolidayWork) 
                {
                    ServiceGroupIdTemp = itemholidaywork.GetTypedColumnValue<Guid>("Id");
                }
                if (ServiceGroupIdTemp != Guid.Empty)
                {
                    ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                    selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                    ProcessSchemaParameterIsDutyGroup = false;
                    return;
                }
            }

            //Приоритетная проверка по Праздничным-рабочим дням - есть ли они на текущую дату?
            EntitySchemaQuery EsqHolidayWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHolidayWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHolidayWorkDate.ChunkSize = 1;
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow.Date));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Less, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow.Date.AddDays(1)));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            EntityCollection CollectionHolidayWorkDate = EsqHolidayWorkDate.GetEntityCollection(_userConnection);
            //Есть рабочий-праздничный день записать ГО и выйти из цикла
            if (CollectionHolidayWorkDate.IsNotEmpty())
            {
                ProcessSchemaParameterIsDutyGroup = true;
                return;
            }

            //можно смотреть по стандартному графику
            EntitySchemaQuery EsqStandartWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqStandartWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqStandartWorkDate.ChunkSize = 2;

            var WeekDayColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code");
            var TimeFromColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeFrom");
            var TimeToColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeTo");
            var orFilterGroup = new EntitySchemaQueryFilterCollection(EsqStandartWorkDate, LogicalOperationStrict.Or);

            EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTypeDay.Name", "Рабочий")); // тип рабочего дня выходной/рабочий
            EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())); // день недели день недели -1
            orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.AddDays(-1).DayOfWeek.ToString())); // день недели день недели -1
            EsqStandartWorkDate.Filters.Add(orFilterGroup);

            EntityCollection CollectionStandartWorkDate = EsqStandartWorkDate.GetEntityCollection(_userConnection);

            if (CollectionStandartWorkDate.IsNotEmpty())
            {
                foreach (var itemstandartwork in CollectionStandartWorkDate)
                {
                    k=k+1;
                    var WeekDay  = itemstandartwork.GetTypedColumnValue<string>(WeekDayColumn.Name);
                    var TimeFrom = itemstandartwork.GetTypedColumnValue<DateTime>(TimeFromColumn.Name);
                    var TimeTo   = itemstandartwork.GetTypedColumnValue<DateTime>(TimeToColumn.Name);

                    if (TimeFrom >= TimeTo) 
                    {
                        TimeTo = TimeTo.AddDays(1);
                    }
                    
                    if (WeekDay != DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())
                    {
                        TimeFrom = TimeFrom.AddDays(-1);TimeTo = TimeTo.AddDays(-1);
                    }

                    // Подходит ли основной график ГО
                    if (CurrentDayTime>TimeFrom && CurrentDayTime < TimeTo)
                    {
                        ServiceGroupIdTemp = itemstandartwork.GetTypedColumnValue<Guid>("Id");
                        break;
                    }
                }
                if (ServiceGroupIdTemp != Guid.Empty)
                {
                    ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                    selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                    ProcessSchemaParameterIsDutyGroup = false;
                }
            }


            if (ServiceGroupIdTemp == Guid.Empty)
            {
                //дежурная группа 
                ProcessSchemaParameterIsDutyGroup = true;
            }
            ProcessSchemaParameter3 = k;
            return;

            /**LEGACY**/
        }

        // Найти ГО дежурную по графику работы
        public void GetExtraServiceGroupBaseOnTimetable()
        {
            _log.Info("GetExtraServiceGroupBaseOnTimetable");
            DateTime CurrentDayTime = DateTime.UtcNow.AddHours(3);

            Guid ServiceGroupId = OLP_DUTY_SERVICE_GROUP; 
            string sheduletype = "";

            if (ServiceGroupId != Guid.Empty)
            {
                EntitySchemaQuery esq = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
                esq.PrimaryQueryColumn.IsAlwaysSelect = true;
                esq.ChunkSize = 1;
                var OlpTypeScheduleWorks = esq.AddColumn("OlpTypeScheduleWorks.Name");
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));
                EntityCollection entityCollection = esq.GetEntityCollection(_userConnection);
                //Идем в цикл если коллекция не пустая
                if (entityCollection.IsNotEmpty()) 
                {
                    foreach (var groupsshedule in entityCollection) 
                    {
                        sheduletype = groupsshedule.GetTypedColumnValue<string>(OlpTypeScheduleWorks.Name);
                        break;
                    }
                }
            }

            Guid ServiceGroupIdTemp = System.Guid.Empty;

            if (sheduletype == "Круглосуточно")
            {
                ServiceGroupIdTemp = ServiceGroupId;
                ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                ProcessSchemaParameterIsDutyGroup = false;
                return;
            }

            //Приоритетная проверка по Праздничным-выходным дням
            EntitySchemaQuery EsqHoliday = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHoliday.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHoliday.ChunkSize = 1;
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));
            // Найти График в праздничные дни
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow));
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow));
            EsqHoliday.Filters.Add(EsqHoliday.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Выходной"));


            EntityCollection CollectionHoliday = EsqHoliday.GetEntityCollection(_userConnection);
            //Есть выходной-праздничный день перейти к следующей ГО
            if (CollectionHoliday.IsNotEmpty())
            {
                ProcessSchemaParameterIsDutyGroup = true;
                return;
            }

            //Приоритетная проверка по Праздничным-рабочим дням
            EntitySchemaQuery EsqHolidayWork = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHolidayWork.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHolidayWork.ChunkSize = 1;
            // Найти График в праздничные дни
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.LessOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
            EsqHolidayWork.Filters.Add(EsqHolidayWork.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            EntityCollection CollectionHolidayWork = EsqHolidayWork.GetEntityCollection(_userConnection);
            //Есть рабочий-праздничный день записать ГО и выйти из цикла
            if (CollectionHolidayWork.IsNotEmpty())
            {
                foreach (var itemholidaywork in CollectionHolidayWork) 
                {
                    ServiceGroupIdTemp = itemholidaywork.GetTypedColumnValue<Guid>("Id");
                }

                if (ServiceGroupIdTemp != Guid.Empty)
                {
                    ProcessSchemaParamServiceGroupId = ServiceGroupIdTemp;
                    selectedServiceGroupId = ProcessSchemaParamServiceGroupId;
                    ProcessSchemaParameterIsDutyGroup = false;
                    return;
                }
            }

            //Приоритетная проверка по Праздничным-рабочим дням - есть ли они на текущую дату?
            EntitySchemaQuery EsqHolidayWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqHolidayWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqHolidayWorkDate.ChunkSize = 1;
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.GreaterOrEqual, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeFrom", DateTime.UtcNow.Date));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Less, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpDatetimeTo", DateTime.UtcNow.Date.AddDays(1)));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpHolidayException:OlpServiceGroupHolidaysDetail:Id].OlpTypeDay.Name", "Рабочий"));
            EsqHolidayWorkDate.Filters.Add(EsqHolidayWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            EntityCollection CollectionHolidayWorkDate = EsqHolidayWorkDate.GetEntityCollection(_userConnection);
            //Есть рабочий-праздничный день записать ГО и выйти из цикла
            if (CollectionHolidayWorkDate.IsNotEmpty())
            {
                ProcessSchemaParameterIsDutyGroup = true;
                return;
            }

            //можно смотреть по стандартному графику
            EntitySchemaQuery EsqStandartWorkDate = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            EsqStandartWorkDate.PrimaryQueryColumn.IsAlwaysSelect = true;
            EsqStandartWorkDate.ChunkSize = 2;

            var WeekDayColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code");
            var TimeFromColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeFrom");
            var TimeToColumn = EsqStandartWorkDate.AddColumn("[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTimeTo");
            var orFilterGroup = new EntitySchemaQueryFilterCollection(EsqStandartWorkDate, LogicalOperationStrict.Or);

            EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpTypeDay.Name", "Рабочий")); // тип рабочего дня выходной/рабочий
            EsqStandartWorkDate.Filters.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "Id", ServiceGroupId));

            orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())); // день недели день недели -1
            orFilterGroup.Add(EsqStandartWorkDate.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpServiceGroupWork:OlpServiceGroupWorkSched:Id].OlpWeekDay.Code", DateTime.UtcNow.AddHours(3).Date.AddDays(-1).DayOfWeek.ToString())); // день недели день недели -1
            EsqStandartWorkDate.Filters.Add(orFilterGroup);

            EntityCollection CollectionStandartWorkDate = EsqStandartWorkDate.GetEntityCollection(_userConnection);

            if (CollectionStandartWorkDate.IsNotEmpty()) 
            {
                foreach (var itemstandartwork in CollectionStandartWorkDate)
                {

                    var WeekDay  = itemstandartwork.GetTypedColumnValue<string>(WeekDayColumn.Name);
                    var TimeFrom = itemstandartwork.GetTypedColumnValue<DateTime>(TimeFromColumn.Name);
                    var TimeTo   = itemstandartwork.GetTypedColumnValue<DateTime>(TimeToColumn.Name);

                    if (TimeFrom >= TimeTo) 
                    {
                        TimeTo = TimeTo.AddDays(1);
                    }
                    
                    if (WeekDay != DateTime.UtcNow.AddHours(3).Date.DayOfWeek.ToString())
                    {
                        TimeFrom = TimeFrom.AddDays(-1);TimeTo = TimeTo.AddDays(-1);
                    }

                    // Подходит ли основной график ГО
                    if (CurrentDayTime>TimeFrom && CurrentDayTime < TimeTo)
                    {
                        ServiceGroupIdTemp = itemstandartwork.GetTypedColumnValue<Guid>("Id");
                        break;
                    }
                }
                if (ServiceGroupIdTemp != Guid.Empty)
                {
                    ProcessSchemaParameterIsDutyGroup = false;
                }
            }


            if (ServiceGroupIdTemp == Guid.Empty)
            {
                //дежурная группа 
                ProcessSchemaParameterIsDutyGroup = true;
            }

            return;
        }

        public void SetFirstLineSupport()
        {
            _log.Info("SetFirstLineSupport");
            string sql = $@"
                UPDATE 
                   Case
                SET 
                    OlpGroupServices = '{selectedServiceGroupId}',
                    OlpSupportLine = '{OLP_OR_FIRST_LINE_SUPPORT}',
                    OlpImportant = '{importancy}',
                    OlpUrgency = '{CASE_URGENCY_TYPE_NOT_URGENT}',
                    OlpIsAuthorVIP = '{clientVip}',
                    Account = '{clientCompanyId}',
                    Category = '{caseCategory}',
                    OlpServiceGroupForOrder = '{ServiceGroupForOrder}' 
                WHERE 
                    Id = '{caseId}'";

            CustomQuery query = new CustomQuery(_userConnection, sql);
            query.Execute();

        }

        public void SetSecondLineSupport()
        {
            _log.Info("SetSecondLineSupport");
            string sql = $@" 
                UPDATE 
                   Case
                SET 
                    OlpGroupServices = '{selectedServiceGroupId}',
                    OlpSupportLine = '{OLP_OR_SECOND_LINE_SUPPORT}',
                    OlpImportant = '{importancy}',
                    OlpUrgency = '{CASE_URGENCY_TYPE_NOT_URGENT}',
                    OlpIsAuthorVIP = '{clientVip}',
                    Account = '{clientCompanyId}',
                    Category = '{caseCategory}',
                    OlpServiceGroupForOrder = '{ServiceGroupForOrder}' 
                WHERE
                    Id = '{caseId}'";

            CustomQuery query = new CustomQuery(_userConnection, sql);
            query.Execute();
        }

        public void SetThirdLineSupport()
        {
            _log.Info("SetThirdLineSupport");

            string sql = $@"
                UPDATE 
                   Case
                SET 
                    OlpGroupServices = '{selectedServiceGroupId}',
                    OlpSupportLine = '{OLP_OR_THIRD_LINE_SUPPORT}',
                    OlpImportant = '{CASE_IMPORTANCY_IMPOTANT}',
                    OlpUrgency = '{CASE_URGENCY_TYPE_NOT_URGENT}',
                    OlpIsAuthorVIP = '{clientVip}',
                    Account = '{clientCompanyId}',
                    Category = '{caseCategory}',
                    OlpServiceGroupForOrder = '{ServiceGroupForOrder}', 
                WHERE 
                    Id = '{caseId}'";
            CustomQuery query = new CustomQuery(_userConnection, sql);
            query.Execute();
 
        }

        /**
         * TODO
         * */
        public void SendBookAutoreply()
        {
            _log.Info("SendBookAutoreply");
            return;
        }

        /**
         * TODO
         * */
        public void SetAutonotification()
        {
            _log.Info("SetAutonotification");
            return;
            // string sql = $@"
            //     UPDATE
            //         Activity
            //     SET 
            //         IsAutoSubmitted = '{true}',
            //     WHERE 
            //         id = '{parentActivityId}'";
            // CustomQuery query = new CustomQuery(_userConnection, sql);
            // query.Execute();
        }

        public void RefreshEmailsAndPhones()
        {

            _log.Info("RefreshEmailsAndPhones");

            /**LEGACY**/

            // обновление/добавление почты
            var emailList = eis.Emails; 
            var IdContact = contactId;

            foreach (var item in emailList) 
            {

                var NameEmail = item.Address;

                if (string.IsNullOrEmpty(NameEmail)) { continue; }	// если нет почты то идти на следующий


                // Существует email?
                EntitySchemaQuery EsqContact = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "ContactCommunication");
                EsqContact.PrimaryQueryColumn.IsAlwaysSelect = true;
                EsqContact.ChunkSize = 1;
                EsqContact.Filters.Add(EsqContact.CreateFilterWithParameters(FilterComparisonType.Equal, "Contact", IdContact));
                EsqContact.Filters.Add(EsqContact.CreateFilterWithParameters(FilterComparisonType.Equal, "Number", NameEmail));
                EsqContact.Filters.Add(EsqContact.CreateFilterWithParameters(FilterComparisonType.Equal, "CommunicationType", Guid.Parse("ee1c85c3-cfcb-df11-9b2a-001d60e938c6")));
                EntityCollection CollectionEmail = EsqContact.GetEntityCollection(_userConnection);

                if (CollectionEmail.IsEmpty())
                {
                    // continue;
                    // Добавление почты контакту
                    var emailContact = _userConnection.EntitySchemaManager.GetInstanceByName("ContactCommunication");
                    var entityemailContact = emailContact.CreateEntity(_userConnection);

                    entityemailContact.UseAdminRights = false;
                    entityemailContact.SetDefColumnValues();
                    entityemailContact.SetColumnValue("ContactId", IdContact );
                    entityemailContact.SetColumnValue("Number", NameEmail);
                    entityemailContact.SetColumnValue("CommunicationTypeId", Guid.Parse("ee1c85c3-cfcb-df11-9b2a-001d60e938c6"));
                    entityemailContact.Save();
                }

            }

            // обновление/добавление телефона
            var listPhone = eis.Phones;
            var IdContact1 = ContactIdForEmailAndPhone;

            foreach (var item1 in listPhone) 
            {
                var NamePhone = item1.Number;
                var Kind = item1.Kind;

                if (string.IsNullOrEmpty(NamePhone)) { continue; }	// если нет телефона то идти на следующий

                var typeIdPhone = Guid.Empty;

                // найти тип телефона
                if (Kind == "mobile") { typeIdPhone = Guid.Parse("d4a2dc80-30ca-df11-9b2a-001d60e938c6"); }
                if (Kind == "work") { typeIdPhone = Guid.Parse("3dddb3cc-53ee-49c4-a71f-e9e257f59e49"); }
                if (Kind == "home") { typeIdPhone = Guid.Parse("0da6a26b-d7bc-df11-b00f-001d60e938c6"); } 
                if (string.IsNullOrEmpty(Kind)) { typeIdPhone = Guid.Parse("21c0d693-9a52-43fa-b7f1-c6d8b53975d4"); }

                // Существует телефон?
                EntitySchemaQuery EsqContactPhone = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "ContactCommunication");
                EsqContactPhone.PrimaryQueryColumn.IsAlwaysSelect = true;
                EsqContactPhone.ChunkSize = 1;
                EsqContactPhone.Filters.Add(EsqContactPhone.CreateFilterWithParameters(FilterComparisonType.Equal, "Contact", IdContact1));
                EsqContactPhone.Filters.Add(EsqContactPhone.CreateFilterWithParameters(FilterComparisonType.Equal, "Number", NamePhone));
                EsqContactPhone.Filters.Add(EsqContactPhone.CreateFilterWithParameters(FilterComparisonType.Equal, "CommunicationType", typeIdPhone));
                EntityCollection CollectionPhone = EsqContactPhone.GetEntityCollection(_userConnection);

                if (CollectionPhone.IsEmpty())
                {
                    // Добавление почты контакту
                    var phoneContact = _userConnection.EntitySchemaManager.GetInstanceByName("ContactCommunication");
                    var entityphoneContact = phoneContact.CreateEntity(_userConnection);

                    entityphoneContact.UseAdminRights = false;
                    entityphoneContact.SetDefColumnValues();
                    entityphoneContact.SetColumnValue("ContactId", IdContact1 );
                    entityphoneContact.SetColumnValue("Number", NamePhone);
                    entityphoneContact.SetColumnValue("CommunicationTypeId", typeIdPhone);
                    entityphoneContact.Save();
                }

            }
            return;
            /**LEGACY**/
        }

        // Найти основную ГО для контакта по компаниям
        public void GetMainServiceGroupForContactBasedOnCompany()
        {
            /**LEGACY**/
            _log.Info("GetMainServiceGroupForContactBasedOnCompany");
            
            //Считать признак поиска в ЕИС
            //Считать Ид. компании 
            Guid companyid = ProcessSchemaParamCompanyFoundId; 
            //Считать Ид. холдинга
            Guid holdingid = ProcessSchemaParamHoldingFoundId;

            //Считать ВИП Платформа
            bool isvipplatform = clientVipPlatform; 

            EntitySchemaQuery esq = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServiceGroup");
            esq.PrimaryQueryColumn.IsAlwaysSelect = true;
            esq.ChunkSize = 1;
            //esq.AddColumn("OlpSgEmail.Name"); //Почтовый ящик string
            esq.AddColumn("[OlpGroupServiceAccount:OlpServiceGroupDetail:Id].OlpAccount"); //Ид.компании-холдинга из детали
            var orFilterGroup = new EntitySchemaQueryFilterCollection(esq, LogicalOperationStrict.Or);

            //Считать данные раздела ГО по основным группам/ВИП платформа
            if (isvipplatform == true)
            {
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "OlpTypeGroupService.Name", "ВИП Платформа"));	
            }
            
            else
            {
                esq.Filters.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "OlpTypeGroupService.Name", "Основная группа"));	

                // Компания || Холдинг
                orFilterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpGroupServiceAccount:OlpServiceGroupDetail:Id].OlpAccount", companyid));
                orFilterGroup.Add(esq.CreateFilterWithParameters(FilterComparisonType.Equal, "[OlpGroupServiceAccount:OlpServiceGroupDetail:Id].OlpAccount", holdingid));
                esq.Filters.Add(orFilterGroup);
            }  

            EntityCollection entityCollection = esq.GetEntityCollection(_userConnection);

            //Идем в цикл если коллекция не пустая
            if (entityCollection.IsNotEmpty()) 
            {
                foreach (var servicegroups in entityCollection) 
                {
                    var servicegroupid = servicegroups.GetTypedColumnValue<Guid>("Id");
                    ProcessSchemaParamServiceGroupId = servicegroupid;
                    selectedServiceGroupId = servicegroupid;
                    ProcessSchemaParameterIsDutyGroup = false;
                }
            }       
            else
            { 
                //Дежурная группа
                ProcessSchemaParameterIsDutyGroup = true;

            }
            /**LEGACY**/
        }

        public bool GetLoadingCheck()
        {
            _log.Info("GetLoadingCheck");

            string sql = $@"
                SELECT 
                    BooleanValue 
                FROM 
                    SysSettingsValue 
                WHERE 
                    SysSettingsId = (
                        SELECT id FROM SysSettings WHERE Code LIKE 'OLPLoadingCheck'
                        )";

            CustomQuery query = new CustomQuery(_userConnection, sql);

            using (var db = _userConnection.EnsureDBConnection())
            {
                using (var reader = query.ExecuteReader(db))
                {
                    if (reader.Read())
                    {
                        return reader.GetColumnValue<bool>("BooleanValue");
                    }
                }
            }
            return false;
        }

        public void CollectServicesForInsertion()
        {
            _log.Info("CollectServicesForInsertion");

            var servicesList = eis.Services;

            bool nagruzka = LoadingCheck;
            int  nagruzkacount = 0;

            var list = new CompositeObjectList<CompositeObject>();

            foreach (var item in servicesList) {
 
                string OrderNumber = item.OrderNumber;
                string ServiceNumber = item.ServiceNumber;
                string TripNumber = item.TripNumber;

                //Если передается пустота - выходим
                if (ServiceNumber == "0") 
                {
                    continue;
                }

                //Проверка существует ли услуга в системе
                EntitySchemaQuery EsqServise = new EntitySchemaQuery(_userConnection.EntitySchemaManager, "OlpServices");
                EsqServise.PrimaryQueryColumn.IsAlwaysSelect = true;
                EsqServise.ChunkSize = 1;
                EsqServise.Filters.Add(EsqServise.CreateFilterWithParameters(FilterComparisonType.Equal, "OlpExternalOrderNumber", OrderNumber));
                EsqServise.Filters.Add(EsqServise.CreateFilterWithParameters(FilterComparisonType.Equal, "OlpExternalServiceId", ServiceNumber));
                EsqServise.Filters.Add(EsqServise.CreateFilterWithParameters(FilterComparisonType.Equal, "OlpExternalTripId", TripNumber));

                EntityCollection CollectionEsqServise = EsqServise.GetEntityCollection(_userConnection);

                if (CollectionEsqServise.IsNotEmpty() && nagruzka == true && nagruzkacount <= 5)
                {
                    var itemo = new CompositeObject();
                    itemo["ServOrderNumber"] = OrderNumber;
                    itemo["ServiceNumb"] = ServiceNumber;
                    itemo["ProcessSchemaParamExtTripId"] = TripNumber;
                    list.Add(itemo);
                    OrderNumber = "";
                    TripNumber = "";
                    ServiceNumber = "";

                    nagruzkacount = nagruzkacount + 1;
                }


                else if (CollectionEsqServise.IsNotEmpty() && (nagruzka == false || nagruzkacount > 5))
                {
                    continue;
                }

                else
                {
                    var itemo = new CompositeObject();
                    itemo["ServOrderNumber"] = OrderNumber;
                    itemo["ServiceNumb"] = ServiceNumber;
                    itemo["ProcessSchemaParamExtTripId"] = TripNumber;
                    list.Add(itemo);
                    OrderNumber = "";
                    TripNumber = "";
                    ServiceNumber = "";
                }
            }

            ServicesToAdd = list;

            ProcessSchemaParamTime = DateTime.Now.ToString("yyyyMMddHHmmssfffffff");

            return;
        }

        public Guid GetFirstLineSupport(Guid role)
        {
            
            _log.Info("GetFirstLineSupport");
            string sql = $@"
                SELECT 
                    sau.Id 
                FROM 
                    SysUserInRole suir
                INNER JOIN
                    SysAdminUnit sau 
                ON 
                    suir.SysUser = sau.Id
                WHERE
                    suir.SysRole = '{role}'
                AND
                    suir.SysRole = '{OLP_OR_FIRST_LINE_SUPPORT}'";

            CustomQuery query = new CustomQuery(_userConnection, sql);

            using (var db = _userConnection.EnsureDBConnection())
            {
                using (var reader = query.ExecuteReader(db))
                {
                    if (reader.Read())
                    {
                        return reader.GetColumnValue<Guid>("Id");
                    }
                }
            }
            return Guid.Empty; 
        }

        public Guid GetThirdLineSupport(Guid role)
        {
            _log.Info("GetThirdLineSupport");
            string sql = $@"
                SELECT 
                    sau.Id 
                FROM 
                    SysUserInRole suir
                INNER JOIN
                    SysAdminUnit sau 
                ON 
                    suir.SysUser = sau.Id
                WHERE
                    suir.SysRole = '{role}'
                AND
                    suir.SysRole = '{OLP_OR_THIRD_LINE_SUPPORT}'
                AND
                    sau.LoggedIn = 1;";

            CustomQuery query = new CustomQuery(_userConnection, sql);

            using (var db = _userConnection.EnsureDBConnection())
            {
                using (var reader = query.ExecuteReader(db))
                {
                    if (reader.Read())
                    {
                        return reader.GetColumnValue<Guid>("Id");
                    }
                }
            }
            return Guid.Empty; 
        }

        public bool SendEisRequest()
        {
            _log.Info("SendEisRequest");
            string id = "";
            string phone = "";
    
            // Email here is 'email' from activity 
            string url = $"http://services.aeroclub.int/bpmintegration/profiles/get-info?Id={id}&Email={email}&Phone={phone}"; 
            
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;

                    string responseBody = client.DownloadString(url);

                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(responseBody)))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(Profile));
                        eis = (Profile)serializer.ReadObject(ms);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }

    public class ServiceGroupElement
    {
        public Guid _serviceGroupId { get; set; }
        public string _timeTableId { get; set; }
    }

    public class Profile
    {
        public int Id { get; set; }
        
        public Name FirstName { get; set; }
        
        public Name LastName { get; set; }
        
        public Name MiddleName { get; set; }
        public Company Company { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        public string Gender { get; set; }
        
        public bool IsAuthorized { get; set; }
        
        public bool IsVip { get; set; }
        
        public bool IsContactPerson { get; set; }
        
        public bool IsVipOnPlatform { get; set; }
        
        public string EmployeeNumber { get; set; }
        
        public string EmployeeLevel { get; set; }
        
        public string ProfileLink { get; set; }
        
        public List<CustomProperty> CustomProperties { get; set; }
        
        public List<Email> Emails { get; set; }
        
        public List<Phone> Phones { get; set; }
        
        public List<Service> Services { get; set; }
        
        public string OrderNumbCheck { get; set; }
    }

    public class Service
    {
        public string ServiceNumber { get; set; }
        public string OrderNumber { get; set; }
        public string JourneyNumber { get; set; }
        public string TripNumber { get; set; }
    }
    
    public class Name
    {
        public string English { get; set; }
        public string Russian { get; set; }
    }

    public class Company
    {
        public string Id { get; set; }
        public Name Name { get; set; }
    }

    public class CustomProperty
    {
        public Property Property { get; set; }
        public Value Value { get; set; }
    }

    public class Property
    {
        public string English { get; set; }
        public string Russian { get; set; }
    }

    public class Value
    {
        public string English { get; set; }
        public string Russian { get; set; }
    }

    public class Email
    {
        public string Kind { get; set; }
        public string Address { get; set; }
    }

    public class Phone
    {
        public string Kind { get; set; }
        public string Number { get; set; }
    }
    
}
  


