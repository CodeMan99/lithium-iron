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
USAGE: life [--help] [--keep] [--adjust-board <x> <y>] [--columns <int>] [--rows <int>] [--speed <sloth|turtle|human|zebra|cheetah>] <filename>

BOARD:

    <filename>            Filename of the desired initial board state

OPTIONS:

    --keep, -k            Never destroy active cells outside of the displayed board
    --adjust-board, -a <x> <y>
                          Adjust every coordinate of the input board
    --columns, -c <int>   Specify number of board columns
    --rows, -r <int>      Specify number of board rows
    --speed, -s <sloth|turtle|human|zebra|cheetah>
                          How fast the game will render each frame [default: human]
    --help                display this list of options.


```

### Complete Example

Given a large enough terminal area, this should display every cell of "Diehard" correctly.

```shell
$ dotnet run --project LithiumIron.App/LithiumIron.App.fsproj -- --rows $LINES --columns $COLUMNS --adjust-board 20 10 --speed zebra --keep boards/methuselahs/diehard.lfe
```

Breakdown:

| Option | Description |
| ------ | ----------- |
| `--rows $LINES` | Set number of rows (Y axis values) equal to the number of available lines. |
| `--columns $COLUMNS` | Set number of columns (X axis values) equal to the number of available columns. |
| `--adjust-board 20 10` | Adjust the initial board by 20 units right and 10 units down from the top. |
| `--speed zebra` | Run at a slightly faster framerate than default. |
| `--keep` | Keep all cells of every generation. |
| `boards/methuselahs/diehards.lfe` | The filename being loaded as initial state. |

## Board Files

Initial board state is stored in a simple text file. Use the `#` (pound)
symbol to indicate active cells. Use any other charactor to indicate empty
cells. Save the file as `<name>.lfe`.

### Example Boards

Please refer to any of the board files found in the `boards` directory.

```
boards
├── generators
│   ├── gosper-glider.lfe
│   └── simkin-glider.lfe
├── methuselahs
│   ├── acorn.lfe
│   ├── diehard.lfe
│   ├── random.lfe
│   ├── r-pentomino.lfe
│   └── taylor-box-target.lfe
├── oscillators
│   ├── beacon.lfe
│   ├── blinker.lfe
│   ├── pentadecathlon.lfe
│   ├── pulsar.lfe
│   └── toad.lfe
├── spaceships
│   ├── glider.lfe
│   ├── heavy-weight.lfe
│   ├── light-weight.lfe
│   └── middle-weight.lfe
└── stills
    ├── beehive.lfe
    ├── block.lfe
    ├── boat.lfe
    ├── loaf.lfe
    └── tub.lfe
```
