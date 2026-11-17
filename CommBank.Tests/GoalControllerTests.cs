using CommBank.Controllers;
using CommBank.Services;
using CommBank.Models;
using CommBank.Tests.Fake;
using Microsoft.AspNetCore.Mvc;

namespace CommBank.Tests;

public class GoalControllerTests
{
    private readonly FakeCollections collections;

    public GoalControllerTests()
    {
        collections = new();
    }

    [Fact]
    public async void GetAll()
    {
        // Arrange
        var goals = collections.GetGoals();
        var users = collections.GetUsers();
        IGoalsService goalsService = new FakeGoalsService(goals, goals[0]);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext = httpContext;
        var result = await controller.Get();

        // Assert
        var index = 0;
        foreach (Goal goal in result)
        {
            Assert.IsAssignableFrom<Goal>(goal);
            Assert.Equal(goals[index].Id, goal.Id);
            Assert.Equal(goals[index].Name, goal.Name);
            index++;
        }
    }

    [Fact]
    public async void Get()
    {
        // Arrange
        var goals = collections.GetGoals();
        var users = collections.GetUsers();
        IGoalsService goalsService = new FakeGoalsService(goals, goals[0]);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext = httpContext;
        var result = await controller.Get(goals[0].Id!);

        // Assert
        Assert.IsAssignableFrom<Goal>(result.Value);
        Assert.Equal(goals[0], result.Value);
        Assert.NotEqual(goals[1], result.Value);
    }

    [Fact]
    public async Task GetForUser()
    {
        // Arrange
        var goals = collections.GetGoals();
        var users = collections.GetUsers();
        var userId = users[0].Id!;

        // The fake goals have no UserId, so without this every UserId assertion
        // below would just compare null with null and could never fail.
        foreach (Goal goal in goals)
        {
            goal.UserId = userId;
        }
        goals[0].Icon = "🏠";

        IGoalsService goalsService = new FakeGoalsService(goals, goals[0]);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext = httpContext;
        var result = await controller.GetForUser(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(goals.Count, result!.Count);

        var index = 0;
        foreach (Goal goal in result)
        {
            Assert.IsAssignableFrom<Goal>(goal);
            Assert.Equal(userId, goal.UserId);
            Assert.Equal(goals[index].Id, goal.Id);
            Assert.Equal(goals[index].Icon, goal.Icon);
            index++;
        }
    }

    [Fact]
    public async Task GetForUserOnlyReturnsThatUsersGoals()
    {
        // Arrange
        var goals = collections.GetGoals();
        var users = collections.GetUsers();
        goals[0].UserId = users[0].Id;
        goals[1].UserId = users[0].Id;
        goals[2].UserId = users[1].Id;

        IGoalsService goalsService = new UserFilteringGoalsService(goals);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext = httpContext;
        var result = await controller.GetForUser(users[0].Id!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.All(result, goal => Assert.Equal(users[0].Id, goal.UserId));
        Assert.DoesNotContain(result, goal => goal.Id == goals[2].Id);
    }

    [Fact]
    public async Task GetForUserWithNoGoalsReturnsEmptyList()
    {
        // Arrange
        var goals = collections.GetGoals();
        var users = collections.GetUsers();
        foreach (Goal goal in goals)
        {
            goal.UserId = users[0].Id;
        }

        IGoalsService goalsService = new UserFilteringGoalsService(goals);
        IUsersService usersService = new FakeUsersService(users, users[0]);
        GoalController controller = new(goalsService, usersService);

        // Act
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
        controller.ControllerContext.HttpContext = httpContext;
        var result = await controller.GetForUser(users[2].Id!);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    // FakeGoalsService.GetForUserAsync ignores the id and returns every goal,
    // so it can't show that the controller asks for the right user.
    // This fake filters by UserId the way the real GoalsService does in MongoDB.
    private class UserFilteringGoalsService : IGoalsService
    {
        private readonly List<Goal> _goals;

        public UserFilteringGoalsService(List<Goal> goals)
        {
            _goals = goals;
        }

        public Task<List<Goal>?> GetForUserAsync(string id) =>
            Task.FromResult<List<Goal>?>(_goals.Where(goal => goal.UserId == id).ToList());

        public Task<List<Goal>> GetAsync() => Task.FromResult(_goals);

        public Task<Goal?> GetAsync(string id) =>
            Task.FromResult(_goals.FirstOrDefault(goal => goal.Id == id));

        public Task CreateAsync(Goal newGoal) => Task.CompletedTask;

        public Task UpdateAsync(string id, Goal updatedGoal) => Task.CompletedTask;

        public Task RemoveAsync(string id) => Task.CompletedTask;
    }
}