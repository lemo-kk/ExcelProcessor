using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// SQL编辑器用户控件
    /// </summary>
    public partial class SqlEditor : UserControl
    {
        private readonly ILogger<SqlEditor> _logger;
        private bool _isFullScreen = false;
        private Window _fullScreenWindow;

        public SqlEditor()
        {
            _logger = null;
            InitializeComponent();
            InitializeSqlEditor();
            SetupKeyBindings();
        }

        public SqlEditor(ILogger<SqlEditor> logger)
        {
            _logger = logger;
            InitializeComponent();
            InitializeSqlEditor();
            SetupKeyBindings();
        }

        /// <summary>
        /// SQL文本属性
        /// </summary>
        public string SqlText
        {
            get => SqlTextEditor?.Text ?? string.Empty;
            set 
            { 
                if (SqlTextEditor != null)
                    SqlTextEditor.Text = value; 
            }
        }

        /// <summary>
        /// SQL文本改变事件
        /// </summary>
        public event EventHandler<string> SqlTextChanged;

        /// <summary>
        /// 格式化请求事件
        /// </summary>
        public event EventHandler FormatRequested;

        /// <summary>
        /// 语法验证请求事件
        /// </summary>
        public event EventHandler<string> ValidateRequested;

        /// <summary>
        /// 初始化SQL编辑器
        /// </summary>
        private void InitializeSqlEditor()
        {
            try
            {
                if (SqlTextEditor == null)
                {
                    _logger?.LogWarning("SqlTextEditor 控件未初始化");
                    return;
                }

                // 设置SQL语法高亮
                var sqlHighlighting = CreateSqlHighlighting();
                SqlTextEditor.SyntaxHighlighting = sqlHighlighting;

                // 设置编辑器选项
                SqlTextEditor.Options.ConvertTabsToSpaces = true;
                SqlTextEditor.Options.IndentationSize = 4;
                SqlTextEditor.Options.EnableHyperlinks = false;
                SqlTextEditor.Options.EnableEmailHyperlinks = false;
                SqlTextEditor.Options.ShowEndOfLine = false;
                SqlTextEditor.Options.ShowSpaces = false;
                SqlTextEditor.Options.ShowTabs = false;

                // 设置行号样式
                SqlTextEditor.ShowLineNumbers = true;
                SqlTextEditor.LineNumbersForeground = new SolidColorBrush(Color.FromRgb(133, 133, 133));

                // 设置选择颜色
                SqlTextEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromArgb(120, 51, 153, 255));
                SqlTextEditor.TextArea.SelectionBorder = new Pen(new SolidColorBrush(Color.FromRgb(51, 153, 255)), 1);

                // 监听光标位置变化
                SqlTextEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;

                _logger?.LogInformation("SQL编辑器初始化完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "初始化SQL编辑器失败");
            }
        }

        /// <summary>
        /// 创建SQL语法高亮定义
        /// </summary>
        private IHighlightingDefinition CreateSqlHighlighting()
        {
            var sqlHighlightingXml = @"<?xml version=""1.0""?>
<SyntaxDefinition name=""SQL"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">
    <Color name=""Comment"" foreground=""#6A9955"" />
    <Color name=""String"" foreground=""#CE9178"" />
    <Color name=""Keyword"" foreground=""#569CD6"" fontWeight=""bold"" />
    <Color name=""Function"" foreground=""#DCDCAA"" />
    <Color name=""Number"" foreground=""#B5CEA8"" />
    <Color name=""Operator"" foreground=""#D4D4D4"" />
    <Color name=""Identifier"" foreground=""#9CDCFE"" />
    
    <RuleSet>
        <!-- 单行注释 -->
        <Span color=""Comment"" begin=""--"" />
        
        <!-- 多行注释 -->
        <Span color=""Comment"" multiline=""true"" begin=""/\*"" end=""\*/"" />
        
        <!-- 字符串 -->
        <Span color=""String"">
            <Begin>'</Begin>
            <End>'</End>
            <RuleSet>
                <Span begin=""''"" end="""" />
            </RuleSet>
        </Span>
        
        <!-- 双引号字符串 -->
        <Span color=""String"">
            <Begin>""</Begin>
            <End>""</End>
            <RuleSet>
                <Span begin=""\"""" end="""" />
            </RuleSet>
        </Span>
        
        <!-- 数字 -->
        <Rule color=""Number"">
            \b0[xX][0-9a-fA-F]+  # hex number
            |    \b
            (    \d+(\.[0-9]+)?   #number with optional floating point
            |    \.[0-9]+         #or just starting with floating point
            )
            ([eE][+-]?[0-9]+)?    # optional exponent
        </Rule>
        
        <!-- SQL关键字 -->
        <Keywords color=""Keyword"">
            <Word>SELECT</Word>
            <Word>FROM</Word>
            <Word>WHERE</Word>
            <Word>INSERT</Word>
            <Word>UPDATE</Word>
            <Word>DELETE</Word>
            <Word>CREATE</Word>
            <Word>ALTER</Word>
            <Word>DROP</Word>
            <Word>TABLE</Word>
            <Word>DATABASE</Word>
            <Word>INDEX</Word>
            <Word>VIEW</Word>
            <Word>PROCEDURE</Word>
            <Word>FUNCTION</Word>
            <Word>TRIGGER</Word>
            <Word>JOIN</Word>
            <Word>INNER</Word>
            <Word>LEFT</Word>
            <Word>RIGHT</Word>
            <Word>FULL</Word>
            <Word>OUTER</Word>
            <Word>CROSS</Word>
            <Word>ON</Word>
            <Word>USING</Word>
            <Word>UNION</Word>
            <Word>INTERSECT</Word>
            <Word>EXCEPT</Word>
            <Word>ORDER</Word>
            <Word>BY</Word>
            <Word>GROUP</Word>
            <Word>HAVING</Word>
            <Word>LIMIT</Word>
            <Word>OFFSET</Word>
            <Word>TOP</Word>
            <Word>DISTINCT</Word>
            <Word>ALL</Word>
            <Word>AND</Word>
            <Word>OR</Word>
            <Word>NOT</Word>
            <Word>IN</Word>
            <Word>EXISTS</Word>
            <Word>BETWEEN</Word>
            <Word>LIKE</Word>
            <Word>IS</Word>
            <Word>NULL</Word>
            <Word>TRUE</Word>
            <Word>FALSE</Word>
            <Word>CASE</Word>
            <Word>WHEN</Word>
            <Word>THEN</Word>
            <Word>ELSE</Word>
            <Word>END</Word>
            <Word>AS</Word>
            <Word>IF</Word>
            <Word>WHILE</Word>
            <Word>BEGIN</Word>
            <Word>COMMIT</Word>
            <Word>ROLLBACK</Word>
            <Word>TRANSACTION</Word>
            <Word>DECLARE</Word>
            <Word>SET</Word>
            <Word>PRINT</Word>
            <Word>RETURN</Word>
            <Word>EXEC</Word>
            <Word>EXECUTE</Word>
        </Keywords>
        
        <!-- SQL函数 -->
        <Keywords color=""Function"">
            <Word>COUNT</Word>
            <Word>SUM</Word>
            <Word>AVG</Word>
            <Word>MAX</Word>
            <Word>MIN</Word>
            <Word>LEN</Word>
            <Word>LENGTH</Word>
            <Word>SUBSTRING</Word>
            <Word>SUBSTR</Word>
            <Word>UPPER</Word>
            <Word>LOWER</Word>
            <Word>TRIM</Word>
            <Word>LTRIM</Word>
            <Word>RTRIM</Word>
            <Word>REPLACE</Word>
            <Word>CONCAT</Word>
            <Word>COALESCE</Word>
            <Word>ISNULL</Word>
            <Word>NULLIF</Word>
            <Word>CAST</Word>
            <Word>CONVERT</Word>
            <Word>GETDATE</Word>
            <Word>NOW</Word>
            <Word>DATEADD</Word>
            <Word>DATEDIFF</Word>
            <Word>YEAR</Word>
            <Word>MONTH</Word>
            <Word>DAY</Word>
            <Word>HOUR</Word>
            <Word>MINUTE</Word>
            <Word>SECOND</Word>
            <Word>ABS</Word>
            <Word>ROUND</Word>
            <Word>CEILING</Word>
            <Word>FLOOR</Word>
            <Word>POWER</Word>
            <Word>SQRT</Word>
            <Word>ROW_NUMBER</Word>
            <Word>RANK</Word>
            <Word>DENSE_RANK</Word>
            <Word>PARTITION</Word>
            <Word>OVER</Word>
        </Keywords>
        
        <!-- 数据类型 -->
        <Keywords color=""Keyword"">
            <Word>INT</Word>
            <Word>INTEGER</Word>
            <Word>BIGINT</Word>
            <Word>SMALLINT</Word>
            <Word>TINYINT</Word>
            <Word>DECIMAL</Word>
            <Word>NUMERIC</Word>
            <Word>FLOAT</Word>
            <Word>REAL</Word>
            <Word>MONEY</Word>
            <Word>SMALLMONEY</Word>
            <Word>VARCHAR</Word>
            <Word>NVARCHAR</Word>
            <Word>CHAR</Word>
            <Word>NCHAR</Word>
            <Word>TEXT</Word>
            <Word>NTEXT</Word>
            <Word>DATE</Word>
            <Word>TIME</Word>
            <Word>DATETIME</Word>
            <Word>DATETIME2</Word>
            <Word>SMALLDATETIME</Word>
            <Word>TIMESTAMP</Word>
            <Word>BINARY</Word>
            <Word>VARBINARY</Word>
            <Word>IMAGE</Word>
            <Word>BIT</Word>
            <Word>UNIQUEIDENTIFIER</Word>
            <Word>XML</Word>
            <Word>JSON</Word>
        </Keywords>
        
        <!-- 操作符 -->
        <Rule color=""Operator"">
            [?,.;()+=\-*/&amp;|^!&lt;&gt;]+
        </Rule>
        
        <!-- 标识符 -->
        <Rule color=""Identifier"">
            \b[\w_][\w\d_]*
        </Rule>
    </RuleSet>
</SyntaxDefinition>";

            using var reader = new StringReader(sqlHighlightingXml);
            using var xmlReader = XmlReader.Create(reader);
            return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
        }

        /// <summary>
        /// 设置键盘快捷键
        /// </summary>
        private void SetupKeyBindings()
        {
            // Ctrl+Shift+F 格式化
            var formatBinding = new KeyBinding(new RelayCommand(_ => FormatSql()), Key.F, ModifierKeys.Control | ModifierKeys.Shift);
            InputBindings.Add(formatBinding);

            // Ctrl+/ 注释切换
            var commentBinding = new KeyBinding(new RelayCommand(_ => ToggleComment()), Key.OemQuestion, ModifierKeys.Control);
            InputBindings.Add(commentBinding);

            // F11 全屏切换
            var fullScreenBinding = new KeyBinding(new RelayCommand(_ => ToggleFullScreen()), Key.F11, ModifierKeys.None);
            InputBindings.Add(fullScreenBinding);
        }

        #region 事件处理

        private void SqlTextEditor_TextChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
            SqlTextChanged?.Invoke(this, SqlTextEditor?.Text ?? string.Empty);
        }

        private void SqlTextEditor_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 右键菜单已在XAML中定义
        }

        private void Caret_PositionChanged(object sender, EventArgs e)
        {
            UpdateLineColumnDisplay();
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            FormatSql();
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            ValidateSql();
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullScreen();
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FontSizeComboBox?.SelectedItem is ComboBoxItem item && 
                int.TryParse(item.Content?.ToString(), out int fontSize) &&
                SqlTextEditor != null)
            {
                SqlTextEditor.FontSize = fontSize;
            }
        }

        #endregion

        #region 右键菜单事件

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            SqlTextEditor?.Cut();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            SqlTextEditor?.Copy();
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            SqlTextEditor?.Paste();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            SqlTextEditor?.SelectAll();
        }

        private void Format_Click(object sender, RoutedEventArgs e)
        {
            FormatSql();
        }

        private void ToggleComment_Click(object sender, RoutedEventArgs e)
        {
            ToggleComment();
        }

        #endregion

        #region 功能方法

        /// <summary>
        /// 格式化SQL语句
        /// </summary>
        private void FormatSql()
        {
            try
            {
                if (SqlTextEditor == null) return;

                StatusText.Text = "正在格式化...";
                StatusText.Foreground = new SolidColorBrush(Colors.Orange);

                var formattedSql = FormatSqlText(SqlTextEditor.Text);
                SqlTextEditor.Text = formattedSql;

                StatusText.Text = "格式化完成";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);

                // 触发外部格式化事件
                FormatRequested?.Invoke(this, EventArgs.Empty);

                _logger?.LogInformation("SQL格式化完成");
            }
            catch (Exception ex)
            {
                StatusText.Text = "格式化失败";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                _logger?.LogError(ex, "SQL格式化失败");
            }
        }

        /// <summary>
        /// 验证SQL语法
        /// </summary>
        private void ValidateSql()
        {
            try
            {
                if (SqlTextEditor == null) return;

                StatusText.Text = "正在验证语法...";
                StatusText.Foreground = new SolidColorBrush(Colors.Orange);

                var validationResult = ValidateSqlSyntax(SqlTextEditor.Text);
                
                if (validationResult.IsValid)
                {
                    SyntaxStatusText.Text = "语法: 正常";
                    SyntaxStatusText.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    StatusText.Text = "语法验证通过";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    SyntaxStatusText.Text = "语法: 错误";
                    SyntaxStatusText.Foreground = new SolidColorBrush(Colors.Red);
                    StatusText.Text = $"语法错误: {validationResult.ErrorMessage}";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                }

                // 触发外部验证事件
                ValidateRequested?.Invoke(this, SqlTextEditor?.Text ?? string.Empty);

                _logger?.LogInformation("SQL语法验证完成: {IsValid}", validationResult.IsValid);
            }
            catch (Exception ex)
            {
                StatusText.Text = "验证失败";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                _logger?.LogError(ex, "SQL语法验证失败");
            }
        }

        /// <summary>
        /// 切换注释
        /// </summary>
        private void ToggleComment()
        {
            try
            {
                if (SqlTextEditor == null) return;
                
                var textArea = SqlTextEditor.TextArea;
                var selection = textArea.Selection;

                if (selection.IsEmpty)
                {
                    // 注释当前行
                    var currentLine = SqlTextEditor.Document.GetLineByNumber(textArea.Caret.Line);
                    var lineText = SqlTextEditor.Document.GetText(currentLine.Offset, currentLine.Length);
                    
                    if (lineText.TrimStart().StartsWith("--"))
                    {
                        // 取消注释
                        var uncommentedText = Regex.Replace(lineText, @"^(\s*)--\s?", "$1");
                        SqlTextEditor.Document.Replace(currentLine.Offset, currentLine.Length, uncommentedText);
                    }
                    else
                    {
                        // 添加注释
                        var indentMatch = Regex.Match(lineText, @"^(\s*)");
                        var indent = indentMatch.Groups[1].Value;
                        var commentedText = $"{indent}-- {lineText.Substring(indent.Length)}";
                        SqlTextEditor.Document.Replace(currentLine.Offset, currentLine.Length, commentedText);
                    }
                }
                else
                {
                    // 注释选中的多行
                    var selectedText = selection.GetText();
                    var lines = selectedText.Split('\n');
                    var processedLines = new string[lines.Length];

                    bool shouldUncomment = lines.Length > 0 && lines[0].TrimStart().StartsWith("--");

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (shouldUncomment)
                        {
                            processedLines[i] = Regex.Replace(lines[i], @"^(\s*)--\s?", "$1");
                        }
                        else
                        {
                            var indentMatch = Regex.Match(lines[i], @"^(\s*)");
                            var indent = indentMatch.Groups[1].Value;
                            processedLines[i] = $"{indent}-- {lines[i].Substring(indent.Length)}";
                        }
                    }

                    var processedText = string.Join("\n", processedLines);
                    SqlTextEditor.Document.Replace(selection.SurroundingSegment.Offset, selection.SurroundingSegment.Length, processedText);
                }

                _logger?.LogDebug("切换SQL注释完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "切换SQL注释失败");
            }
        }

        /// <summary>
        /// 切换全屏模式
        /// </summary>
        private void ToggleFullScreen()
        {
            try
            {
                if (!_isFullScreen)
                {
                    // 进入全屏模式
                    _fullScreenWindow = new Window
                    {
                        Title = "SQL编辑器 - 全屏模式",
                        WindowState = WindowState.Maximized,
                        WindowStyle = WindowStyle.None,
                        Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                        Owner = Application.Current.MainWindow
                    };

                    // 创建全屏编辑器副本
                    var fullScreenEditor = new SqlEditor(_logger)
                    {
                        SqlText = this.SqlText
                    };

                    // 同步文本变化
                    fullScreenEditor.SqlTextChanged += (s, text) => this.SqlText = text;

                    _fullScreenWindow.Content = fullScreenEditor;
                    _fullScreenWindow.KeyDown += (s, e) =>
                    {
                        if (e.Key == Key.Escape || e.Key == Key.F11)
                        {
                            ToggleFullScreen();
                        }
                    };

                    _fullScreenWindow.ShowDialog();
                }
                else
                {
                    // 退出全屏模式
                    _fullScreenWindow?.Close();
                }

                _isFullScreen = !_isFullScreen;
                FullScreenButton.Content = _isFullScreen ? "🗗 退出全屏" : "⛶ 全屏";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "切换全屏模式失败");
            }
        }

        /// <summary>
        /// 更新状态栏
        /// </summary>
        private void UpdateStatusBar()
        {
            try
            {
                if (SqlTextEditor != null)
                {
                    LengthText.Text = $"长度: {SqlTextEditor.Text.Length}";
                    UpdateLineColumnDisplay();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新状态栏失败");
            }
        }

        /// <summary>
        /// 更新行列显示
        /// </summary>
        private void UpdateLineColumnDisplay()
        {
            try
            {
                if (SqlTextEditor != null)
                {
                    var line = SqlTextEditor.TextArea.Caret.Line;
                    var column = SqlTextEditor.TextArea.Caret.Column;
                    LineColumnText.Text = $"行: {line}, 列: {column}";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "更新行列显示失败");
            }
        }

        /// <summary>
        /// 简单的SQL格式化
        /// </summary>
        private string FormatSqlText(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            var formatted = sql;

            // 基本格式化规则
            var keywords = new[]
            {
                "SELECT", "FROM", "WHERE", "JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN",
                "ORDER BY", "GROUP BY", "HAVING", "UNION", "INSERT", "UPDATE", "DELETE",
                "CREATE", "ALTER", "DROP", "AND", "OR"
            };

            foreach (var keyword in keywords)
            {
                formatted = Regex.Replace(formatted, 
                    $@"\b{keyword}\b", 
                    $"\n{keyword}", 
                    RegexOptions.IgnoreCase);
            }

            // 清理多余的空行和空格
            formatted = Regex.Replace(formatted, @"\n\s*\n", "\n");
            formatted = Regex.Replace(formatted, @"^\s+", "", RegexOptions.Multiline);

            return formatted.Trim();
        }

        /// <summary>
        /// 简单的SQL语法验证
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateSqlSyntax(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return (false, "SQL语句不能为空");
            }

            // 基本语法检查
            var trimmedSql = sql.Trim().ToUpper();

            // 检查括号匹配
            int openParens = 0;
            foreach (char c in sql)
            {
                if (c == '(') openParens++;
                else if (c == ')') openParens--;
                if (openParens < 0) return (false, "右括号多于左括号");
            }
            if (openParens > 0) return (false, "左括号多于右括号");

            // 检查引号匹配
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            for (int i = 0; i < sql.Length; i++)
            {
                char c = sql[i];
                if (c == '\'' && !inDoubleQuote)
                {
                    if (i + 1 < sql.Length && sql[i + 1] == '\'')
                    {
                        i++; // 跳过转义的引号
                    }
                    else
                    {
                        inSingleQuote = !inSingleQuote;
                    }
                }
                else if (c == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                }
            }
            if (inSingleQuote) return (false, "单引号未闭合");
            if (inDoubleQuote) return (false, "双引号未闭合");

            return (true, "");
        }

        #endregion
    }

    /// <summary>
    /// 简单的命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}