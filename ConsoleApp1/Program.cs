using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SnakeGame
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new SnakeGameApp();
            app.Run();
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("\nЧто бы продолжить нажмите <Enter>...");
                Console.ReadLine();
            }
        }
    }

    public class SnakeGameApp
    {
        private readonly int delayBetweenGames = 1500;

        public void Run()
        {
            var finished = false;
            while (!finished)
            {
                var game = new Game();
                game.Play();
                finished = game.Quit;
                if (game.Lost)
                {
                    Console.WriteLine("\nВы проиграли.");
                    Thread.Sleep(delayBetweenGames);
                    Console.Clear();
                }
            }
        }
    }

    public class Game
    {
        private readonly eHeading initialHeading = eHeading.Up;
        private DirectionMap directionMap = Board.DirectionMap;

        private Board board;
        public bool Lost { get; private set; } = false;
        public bool Quit { get; private set; } = false;

        public Game(int snakeLength = 5)
        {
            board = new Board();
            var head = board.Center;
            board.Add(new Snake(head, initBody(head, snakeLength - 1), directionMap.Get(initialHeading)));
            board.AddFood();
            board.Draw();
        }

        public void Play()
        {
            while (!Lost && !Quit)
            {
                ConsoleKeyInfo input;
                if (Console.KeyAvailable)
                {
                    input = Console.ReadKey(true); //добавим конструкцию try/catch что бы консоль игнорировала любую кнопку кроме кнопок движенрия
                                                   // и невыбрасывала исключене, кроме кнопки Esc? которая завершит игру
                    try
                    {
                        var direction = directionMap.Get(input.KeyChar);
                        board.DoTurn(direction);
                        Lost = board.HasCollided;
                    }
                    catch
                    {
                        Quit = input.Key == ConsoleKey.Escape;
                    }
                }
                else
                {
                    board.DoTurn();
                    Lost = board.HasCollided;
                }
            }
        }

        private LinkedList<Cell> initBody(Cell head, int bodyLength)
        {
            var body = new LinkedList<Cell>();
            var current = board.BottomNeighbor(head);
            for (var i = 0; i < bodyLength; i++)
            {
                body.AddLast(current);
                current.SetBody(bodyLength - i);
                current = board.BottomNeighbor(current);
            }
            return body;
        }
    }

    public class Board
    {
        public static DirectionMap DirectionMap { get; } = new DirectionMap();

        private readonly string instructions = "Правила: Избегайте ударов об стену и свой хвостом. Змейка растет когда Вы поедаете пищу (°).";
        private readonly string commandBase = "Управление: {0}, Esc: Выход\n";
        private readonly string lengthBase = "Длина: {0}\n";

        private Cell[,] grid;
        private Random random = new Random();
        private int leftEdgeX => 0;
        private int rightEdgeX => Width - 1;
        private int topEdgeY => 0;
        private int bottomEdgeY => Height - 1;

        public Snake Snake { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public Cell Current => Snake.Head;
        public Cell Center => get(Width / 2, Height / 2);
        public Cell Food { get; private set; }
        public bool HasCollided { get; private set; }

        public Board(int width = 90, int height = 25)
        {
            Width = width;
            Height = height;
            grid = new Cell[Width, Height];
            initGrid();
        }

        //Комангда для продолжения движения в заданном направлении
        public void DoTurn()
        {
            doTurn(Snake.Direction, getDestination(Snake.Direction));
        }

        public void DoTurn(Direction direction)
        {
            doTurn(direction, getDestination(direction));
        }

        public void Draw()
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine(ToString());
        }

        public void Add(Snake snake) => Snake = snake;
        public void AddFood() => randomCell().SetFood();

        public Cell TopNeighbor(Cell cell) => grid[cell.X, cell.Y - 1];
        public Cell RightNeighbor(Cell cell) => grid[cell.X + 1, cell.Y];
        public Cell BottomNeighbor(Cell cell) => grid[cell.X, cell.Y + 1];
        public Cell LeftNeighbor(Cell cell) => grid[cell.X - 1, cell.Y];

        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    sb.Append(grid[x, y].Value);
                }
                sb.Append("\n"); 
            }
            sb.AppendFormat(lengthBase, Snake.Length);
            sb.AppendLine(instructions);
            sb.AppendFormat(commandBase, DirectionMap.ToString());
            return sb.ToString();
        }

        private Cell get(int x, int y) => grid[x, y];

        private void add(Cell cell) => grid[cell.X, cell.Y] = cell;

        private bool isBorder(Cell cell) => cell.X == leftEdgeX || cell.X >= rightEdgeX
                                         || cell.Y == topEdgeY || cell.Y >= bottomEdgeY;

        private void doTurn(Direction direction, Cell target)
        {
            if (isLegalMove(direction, target))
            {
                Snake.Move(direction, target);

                if (Snake.HasEaten)
                {
                    Snake.Grow(getNewTail());
                    AddFood();
                }

                Draw();
            }
        }

        private bool isLegalMove(Direction direction, Cell target)
        {
            if (direction.IsOpposite(Snake.Direction))
            {
                return false;
            }

            HasCollided = target.IsForbidden;

            return !HasCollided;
        }

        private Cell getDestination(Direction direction) => getDirectionalNeighbor(Snake.Head, direction);

        private Cell getNewTail() => getDirectionalNeighbor(Snake.Tail, Snake.Direction.Opposite);

        private Cell getDirectionalNeighbor(Cell cell, Direction direction)
        {
            var neighbor = new Cell(-1, -1); //initialize to dummy cell

            if (direction.IsUp)
            {
                neighbor = TopNeighbor(cell);
            }
            else if (direction.IsRight)
            {
                neighbor = RightNeighbor(cell);
            }
            else if (direction.IsDown)
            {
                neighbor = BottomNeighbor(cell);
            }
            else if (direction.IsLeft)
            {
                neighbor = LeftNeighbor(cell);
            }

            return neighbor;
        }

        private Cell randomCell()
        {
            bool isEmpty;
            var cell = new Cell(-1, -1); //initialize to dummy cell
            do
            {
                cell = grid[random.Next(Width), random.Next(Height)];
                isEmpty = cell.IsEmpty;
            } while (!isEmpty);

            return cell;
        }

        private void initGrid()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = new Cell(x, y);

                    add(cell);

                    if (isBorder(cell))
                    {
                        cell.SetBorder();
                    }
                    else
                    {
                        cell.SetEmpty();
                    }
                }
            }
        }
    }

    
    public enum eHeading
    {
        Up = 0,
        Right = 90,
        Down = 180,
        Left = 270
    }

    public class Direction
    {
        public eHeading Heading { get; private set; }
        public char KeyPress { get; private set; }
        public char HeadToken { get; private set; }
        public int Degrees => (int)Heading;
        public Direction Opposite => Board.DirectionMap.Get((eHeading)(Degrees >= 180 ? Degrees - 180 : Degrees + 180));

        public bool IsUp => Heading == eHeading.Up;
        public bool IsRight => Heading == eHeading.Right;
        public bool IsDown => Heading == eHeading.Down;
        public bool IsLeft => Heading == eHeading.Left;

        public Direction(eHeading vector, char keyPress, char headToken)
        {
            Heading = vector;
            KeyPress = keyPress;
            HeadToken = headToken;
        }

        public bool IsOpposite(Direction dir) => Math.Abs(Degrees - dir.Degrees) == 180;

        public bool IsSame(Direction dir) => Heading == dir.Heading;

        public string ToCommand() => $"{KeyPress}: {Heading}";
    }

    public class DirectionMap
    {
        private Dictionary<char, Direction> _directionKeys;
        private Dictionary<char, Direction> directionKeys
        {
            get
            {
                _directionKeys = _directionKeys ?? directionKeyMap();
                return _directionKeys;
            }
        }

        private Dictionary<eHeading, Direction> _directionVectors;
        private Dictionary<eHeading, Direction> directionVectors
        {
            get
            {
                _directionVectors = _directionVectors ?? directionVectorMap();
                return _directionVectors;
            }
        }

        public Direction Get(char c)
        {
            if (directionKeys.TryGetValue(c, out Direction direction))
            {
                return direction;
            }
            else
            {
                throw new Exception($"{c} указано неверное направление.");
            }
        }

        public Direction Get(eHeading vector)
        {
            if (directionVectors.TryGetValue(vector, out Direction direction))
            {
                return direction;
            }
            else
            {
                throw new Exception($"Вектор {vector.ToString()} отстутствует на игровом столе.");
            }
        }

        public override string ToString() => string.Join(", ", directionVectors.Select(v => v.Value.ToCommand()));

        private Dictionary<eHeading, Direction> directionVectorMap()
        {
            return new Dictionary<eHeading, Direction>
            {
                {eHeading.Left, new Direction(eHeading.Left , 'a', '<')},
                {eHeading.Right, new Direction(eHeading.Right,'d', '>') },
                {eHeading.Down, new Direction(eHeading.Down, 's', 'v') },
                {eHeading.Up, new Direction(eHeading.Up, 'w', '^') }
            };
        }

        private Dictionary<char, Direction> directionKeyMap() => directionVectors.ToDictionary(d => d.Value.KeyPress, d => d.Value);
    }

    public class Cell
    {
        private readonly char unitializedToken = char.MinValue;
        private readonly char emptyToken = ' ';
        private readonly char borderToken = '*';
        private readonly char bodyToken = '#';
        private readonly char foodToken = '°';

        private int remaining;

        public int X { get; private set; }
        public int Y { get; private set; }
        public char Value { get; private set; }

        public bool IsBorder => Value == borderToken;
        public bool IsBody => Value == bodyToken;
        public bool IsFood => Value == foodToken;
        public bool IsEmpty => Value == emptyToken || Value == unitializedToken;
        public bool IsForbidden => IsBorder || IsBody;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void SetEmpty() => Update(emptyToken);

        public void SetHead(char headToken)
        {
            Update(headToken);
        }

        public void SetBody(int length)
        {
            Update(bodyToken);
            remaining = length;
        }

        public void SetBorder() => Update(borderToken);

        public void SetFood() => Update(foodToken);

        public void Update(char newVal) => Value = newVal;

        public void Decay()
        {
            if (--remaining == 0)
            {
                SetEmpty();
            }
        }

        public override string ToString() => $"{X}, {Y}";
    }

    public class Snake
    {
        private readonly int maxSpeed = 4;
        private readonly int delayMultiplier = 100;

        public Cell Head { get; private set; }
        public LinkedList<Cell> Body { get; private set; }
        public Cell Tail => Body.Last();
        public Direction Direction { get; private set; }
        public int Length => BodyLength + 1;
        public int BodyLength => Body.Count; 
        public int Speed { get; private set; }
        public bool HasEaten { get; private set; }

        public Snake(Cell head, LinkedList<Cell> body, Direction initialHeading, int speed = 1)
        {
            Head = head;
            Body = body;
            Speed = Math.Min(speed, maxSpeed);
            Direction = initialHeading;
            Head.SetHead(Direction.HeadToken);
        }

        public void Move(Direction direction, Cell nextHead)
        {
            var originalHead = Head;

            Direction = direction;

            HasEaten = false; 

            if (nextHead.IsFood)
            {
                Eat();
            }

            Head = nextHead;

            Head.SetHead(direction.HeadToken);

            moveBody(originalHead);

            pause();
        }

        public void Eat()
        {
            HasEaten = true;
        }

        public void Grow(Cell newTail)
        {
            newTail.SetBody(1);
            Body.AddLast(newTail);
        }

        private void moveBody(Cell originalHead)
        {
            foreach (var cell in Body)
            {
                cell.Decay(); 
            }
            Body.AddFirst(originalHead);
            originalHead.SetBody(BodyLength - 1);
            Body.RemoveLast();
        }

        private void pause() => Thread.Sleep(maxSpeed - Speed + 1 * delayMultiplier);
    }
}