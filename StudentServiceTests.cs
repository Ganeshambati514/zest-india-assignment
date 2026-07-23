using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StudentManagementSystem.Application.DTOs;
using StudentManagementSystem.Application.Exceptions;
using StudentManagementSystem.Application.Interfaces;
using StudentManagementSystem.Application.Services;
using StudentManagementSystem.Domain.Entities;
using ValidationException = StudentManagementSystem.Application.Exceptions.ValidationException;

namespace StudentManagementSystem.Tests;

public class StudentServiceTests
{
    private readonly Mock<IStudentRepository> _repositoryMock;
    private readonly StudentService _sut; // system under test

    public StudentServiceTests()
    {
        _repositoryMock = new Mock<IStudentRepository>();
        var loggerMock = new Mock<ILogger<StudentService>>();
        _sut = new StudentService(_repositoryMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task GetAllStudentsAsync_ReturnsAllStudents_MappedToDto()
    {
        var students = new List<Student>
        {
            new() { Id = 1, Name = "Alice", Email = "alice@test.com", Age = 20, Course = "CS" },
            new() { Id = 2, Name = "Bob", Email = "bob@test.com", Age = 22, Course = "IT" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(students);

        var result = await _sut.GetAllStudentsAsync();

        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetStudentByIdAsync_ThrowsNotFoundException_WhenStudentDoesNotExist()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Student?)null);

        var act = () => _sut.GetStudentByIdAsync(99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddStudentAsync_ThrowsValidationException_WhenEmailAlreadyExists()
    {
        var dto = new CreateStudentDto { Name = "Alice", Email = "alice@test.com", Age = 20, Course = "CS" };
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email))
            .ReturnsAsync(new Student { Id = 1, Email = dto.Email });

        var act = () => _sut.AddStudentAsync(dto);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task AddStudentAsync_ReturnsCreatedStudent_WhenEmailIsUnique()
    {
        var dto = new CreateStudentDto { Name = "Charlie", Email = "charlie@test.com", Age = 21, Course = "ECE" };
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Student?)null);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Student>()))
            .ReturnsAsync((Student s) => { s.Id = 10; return s; });

        var result = await _sut.AddStudentAsync(dto);

        result.Id.Should().Be(10);
        result.Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task DeleteStudentAsync_ThrowsNotFoundException_WhenStudentDoesNotExist()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Student?)null);

        var act = () => _sut.DeleteStudentAsync(5);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateStudentAsync_UpdatesAndReturnsStudent_WhenValid()
    {
        var existing = new Student { Id = 3, Name = "Old", Email = "old@test.com", Age = 19, Course = "Math" };
        var dto = new UpdateStudentDto { Name = "New", Email = "new@test.com", Age = 20, Course = "Physics" };

        _repositoryMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Student?)null);

        var result = await _sut.UpdateStudentAsync(3, dto);

        result.Name.Should().Be("New");
        result.Email.Should().Be("new@test.com");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Student>()), Times.Once);
    }
}
