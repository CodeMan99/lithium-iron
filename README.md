# Lithum Iron

F# implementation of _Conway's Game of Life_.

## Inspiration

[Stop Writing Classes](https://youtu.be/o9pEzgHorH0?si=yTquIv78QWMXjHhu&t=1040) - A fantastic
Python talk by Jack Diederich. Applicable to all multiple paradigm programming languages.

## Development Setup

Make use of the [devcontainer](https://code.visualstudio.com/docs/devcontainers/containers)
included in this project! Once the container is up, use the `dotnet` CLI to build the project.

```shell
$ dotnet tool restore
$ dotnet restore
$ dotnet build
```

## Running the Project

Copy an initial board file to the project directory.

```shell
$ cp boards/generators/gosper-glider.lfe LithiumIron.App/board.lfe
$ cd LithiumIron.App
$ dotnet run
```

The `board.lfe` filename is hardcoded. Use `^C` to stop the program.

## Board Files

Initial board state is stored in a simple text file. Use the `#` (pound)
symbol to indicate active cells. Use any other charactor to indicate empty
cells. Save the file as `<name>.lfe`.
