using System.Net.Http.Json;
using System.Text.Json.Serialization;

class Program
{
    private static readonly HttpClient httpClient = new()
    {
        BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
    };

    static async Task Main()
    {
        Console.Write("Введите ID пользователя: ");
        if (!int.TryParse(Console.ReadLine(), out int userId))
        {
            Console.WriteLine("Некорректный ID пользователя.");
            return;
        }

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Показать задачи пользователя");
            Console.WriteLine("2. Найти задачу по ID");
            Console.WriteLine("3. Создать задачу");
            Console.WriteLine("4. Изменить статус задачи");
            Console.WriteLine("5. Удалить задачу");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите действие: ");

            string? choice = Console.ReadLine();
            Console.WriteLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ShowUserTodosAsync(userId);
                        break;
                    case "2":
                        await FindTodoByIdAsync();
                        break;
                    case "3":
                        await CreateTodoAsync(userId);
                        break;
                    case "4":
                        await ChangeTodoStatusAsync();
                        break;
                    case "5":
                        await DeleteTodoAsync();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Неизвестное действие.");
                        break;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Сетевая ошибка: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Запрос был отменён или истекло время ожидания.");
            }
        }
    }

    private static async Task ShowUserTodosAsync(int userId)
    {
        using var response = await httpClient.GetAsync($"todos?userId={userId}");

        if (!response.IsSuccessStatusCode)
        {
            await PrintErrorAsync(response);
            return;
        }

        List<Todo>? todos = await response.Content.ReadFromJsonAsync<List<Todo>>();

        if (todos is null || todos.Count == 0)
        {
            Console.WriteLine("Задачи не найдены.");
            return;
        }

        foreach (Todo todo in todos)
        {
            Console.WriteLine($"ID: {todo.Id}");
            Console.WriteLine($"Название: {todo.Title}");
            Console.WriteLine($"Статус: {(todo.Completed ? "выполнена" : "не выполнена")}");
            Console.WriteLine(new string('-', 40));
        }
    }

    private static async Task FindTodoByIdAsync()
    {
        Console.Write("Введите ID задачи: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Некорректный ID задачи.");
            return;
        }

        using var response = await httpClient.GetAsync($"todos/{id}");

        if (!response.IsSuccessStatusCode)
        {
            await PrintErrorAsync(response);
            return;
        }

        Todo? todo = await response.Content.ReadFromJsonAsync<Todo>();

        if (todo is null)
        {
            Console.WriteLine("Задача не найдена или ответ пустой.");
            return;
        }

        Console.WriteLine($"ID: {todo.Id}");
        Console.WriteLine($"Пользователь: {todo.UserId}");
        Console.WriteLine($"Название: {todo.Title}");
        Console.WriteLine($"Статус: {(todo.Completed ? "выполнена" : "не выполнена")}");
    }

    private static async Task CreateTodoAsync(int userId)
    {
        Console.Write("Введите название задачи: ");
        string? title = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("Название задачи не может быть пустым.");
            return;
        }

        bool completed = ReadBool("Задача выполнена? (y/n): ");

        var newTodo = new Todo
        {
            UserId = userId,
            Title = title.Trim(),
            Completed = completed
        };

        using var response = await httpClient.PostAsJsonAsync("todos", newTodo);

        if (!response.IsSuccessStatusCode)
        {
            await PrintErrorAsync(response);
            return;
        }

        Todo? createdTodo = await response.Content.ReadFromJsonAsync<Todo>();

        if (createdTodo is null)
        {
            Console.WriteLine("Задача создана, но ответ не удалось прочитать.");
            return;
        }

        Console.WriteLine($"Создана задача #{createdTodo.Id}");
        Console.WriteLine($"Название: {createdTodo.Title}");
        Console.WriteLine($"Статус: {(createdTodo.Completed ? "выполнена" : "не выполнена")}");
        Console.WriteLine("Примечание: JSONPlaceholder не сохраняет изменения на сервере.");
    }

    private static async Task ChangeTodoStatusAsync()
    {
        Console.Write("Введите ID задачи: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Некорректный ID задачи.");
            return;
        }

        bool completed = ReadBool("Новый статус — выполнена? (y/n): ");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"todos/{id}")
        {
            Content = JsonContent.Create(new { completed })
        };

        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            await PrintErrorAsync(response);
            return;
        }

        Todo? updatedTodo = await response.Content.ReadFromJsonAsync<Todo>();

        if (updatedTodo is null)
        {
            Console.WriteLine("Статус изменён, но ответ не удалось прочитать.");
            return;
        }

        Console.WriteLine($"Задача #{updatedTodo.Id} обновлена.");
        Console.WriteLine($"Название: {updatedTodo.Title}");
        Console.WriteLine($"Новый статус: {(updatedTodo.Completed ? "выполнена" : "не выполнена")}");
        Console.WriteLine("Примечание: JSONPlaceholder не сохраняет изменения на сервере.");
    }

    private static async Task DeleteTodoAsync()
    {
        Console.Write("Введите ID задачи для удаления: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Некорректный ID задачи.");
            return;
        }

        using var response = await httpClient.DeleteAsync($"todos/{id}");

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Задача #{id} успешно удалена (имитация).");
            Console.WriteLine("Примечание: JSONPlaceholder не сохраняет изменения на сервере.");
        }
        else
        {
            await PrintErrorAsync(response);
        }
    }

    private static async Task PrintErrorAsync(HttpResponseMessage response)
    {
        Console.WriteLine($"Ошибка HTTP: {(int)response.StatusCode} {response.StatusCode}");

        string content = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(content))
        {
            Console.WriteLine($"Ответ сервера: {content}");
        }
    }


    private static bool ReadBool(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (input is "y" or "yes" or "д" or "да" or "1")
                return true;

            if (input is "n" or "no" or "н" or "нет" or "0")
                return false;

            Console.WriteLine("Введите y/n.");
        }
    }
}

public class Todo
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }
}