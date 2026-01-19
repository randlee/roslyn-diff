def calculate_sum(numbers):
    """Calculate the sum of a list of numbers."""
    total = 0
    for num in numbers:
        if num > 0:
            total += num
    return total


def greet(name):
    """Greet a person by name."""
    if name:
        print(f"Hello, {name}!")
    else:
        print("Hello, World!")
