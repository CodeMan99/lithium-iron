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

Choose an initial board file to pass into the appliction.

```shell
$ cd LithiumIron.App
$ dotnet run -- ../boards/generators/gosper-glider.lfe
```

Use `^C` to stop the program.

## Usage

The application has the follow command line options. The board filename argument is required.

```
USAGE: life [--help] [--keep] [--adjust-board <x> <y>] [--columns <int>] [--rows <int>] <filename>

BOARD:

    <filename>            Filename of the desired initial board state

OPTIONS:

    --keep, -k            Never destroy active cells outside of the displayed board
    --adjust-board, -a <x> <y>
                          Adjust every coordinate of the input board
    --columns, -c <int>   Specify number of board columns
    --rows, -r <int>      Specify number of board rows
    --help                display this list of options.
```

## Board Files

Initial board state is stored in a simple text file. Use the `#` (pound)
symbol to indicate active cells. Use any other charactor to indicate empty
cells. Save the file as `<name>.lfe`.
