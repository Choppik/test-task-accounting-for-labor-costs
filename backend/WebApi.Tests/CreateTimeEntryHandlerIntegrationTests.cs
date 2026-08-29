using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.MongoDb;
using WebApi.Commands;
using WebApi.Handlers.Timesheets;
using WebApi.Models;
using WebApi.Services;
using Xunit;

namespace WebApi.Tests
{ 
    public class CreateTimeEntryHandlerIntegrationTests
    {
        // Контейнер будет жить столько же, сколько живет класс тестов
        private readonly MongoDbContainer _mongoContainer = new MongoDbBuilder()
            .WithImage("mongo:6.0") // Тот же образ, что и в продакшене
            .Build();
    
        private IMongoDatabase _testDatabase;
        private Mock<ITimeEntryLimitService> _mockLimitService;
        private string _employeeId = ObjectId.GenerateNewId().ToString(); // Создаем новый ObjectId

        // Настройка перед всеми тестами в классе
        public async Task InitializeAsync()
        {
            await _mongoContainer.StartAsync();
    
            // Получаем строку подключения от контейнера (порт будет случайным)
            var client = new MongoClient(_mongoContainer.GetConnectionString());
    
            // Создаем уникальную БД для этого запуска тестов
            _testDatabase = client.GetDatabase($"TestDb_{Guid.NewGuid()}");
    
            // НАСТРОЙКА ДАННЫХ (Seed Data)
            // Это заменяет все моки коллекций и курсоров
            var employeesCollection = _testDatabase.GetCollection<Employee>("Employees");
            var projectsCollection = _testDatabase.GetCollection<Project>("Projects");

            // 1. Создаем сотрудника без актуальной ставки (для проверки твоей ошибки)
            var employee = new Employee
            {
                Id = _employeeId, // Используем ObjectId вместо строкового идентификатора
                FullName = "Иван Иванов",
                SalaryHistory = new List<SalaryHistory>
                {
                    new SalaryHistory
                    {
                        EffectiveFrom = DateTime.UtcNow.AddDays(10),
                        HourlyRate = 500m
                    }
                }
            };
            await employeesCollection.InsertOneAsync(employee);

            // 2. Создаем проект
            var projectId = ObjectId.GenerateNewId().ToString(); // Создаем новый ObjectId
            var project = new Project
            {
                Id = projectId,
                Name = "Test Project",
                StartDate = DateTime.UtcNow.AddDays(-10)
            };
            await projectsCollection.InsertOneAsync(project);
    
            // 3. Мокаем только бизнес-логику (лимиты), так как это не БД
            _mockLimitService = new Mock<ITimeEntryLimitService>();
            var checkResult = new TimeEntryCheckResult
            {
                IsValid = true,
                ExistingHours = 0,
                TotalHoursAfterAdd = 8,
                ErrorMessage = null
            };
            _mockLimitService.Setup(x => x.CheckHoursLimitAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(checkResult);
        }
    
        [Fact]
        public async Task Handle_WhenEmployeeHasNoRate_ThrowsException()
        {
            // Arrange
            await InitializeAsync(); // Поднимаем БД и кладем данные
    
            var handler = new CreateTimeEntryHandler(_mockLimitService.Object, _testDatabase);
            var command = new CreateTimeEntryCommand
            {
                EmployeeId = _employeeId,
                Date = DateTime.UtcNow,
                Hours = 8
            };
    
            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    
            Assert.NotNull(ex);
            Assert.Contains("rate", ex.Message.ToLower()); // Проверяем текст ошибки
        }
    
        // Очистка после всех тестов
        public async ValueTask DisposeAsync()
        {
            await _mongoContainer.DisposeAsync();
        }
    }
}
