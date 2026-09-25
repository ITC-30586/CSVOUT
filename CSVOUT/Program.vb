'
'   CSV_OUT     PostgreSQLのViewデータをCSV出力する
'   2026/09/25
Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Xml


Module Program

    'INI
    Private DBIP As String = String.Empty           'DB IPアドレス           :133.222.186.64(開発環境)
    Private DBPO As String = String.Empty           'DB ポート番号           :25432
    Private DBNM As String = String.Empty           'DB データベース名       :dbtokyu
    Private DBID As String = String.Empty           'DB ユーザーID           :tokyu
    Private DBPW As String = String.Empty           'DB パスワード           :trace
    'Private APPATH As String = String.Empty         'postgresアプリケーションパス:C:\Program Files\PostgreSQL\15\bin
    Private OutForder As String = String.Empty      '出力先フォルダ



    Sub Main()
        '起動パス取得
        Dim MyPath As String = AppContext.BaseDirectory

        Dim batPath As String = MyPath & "\export_view.bat"
        Dim psi As New ProcessStartInfo()

        'INI.XMLから設定取得
        Call Init_xml()
        'batファイル作成
        Call Make_batFile()

        'batファイルのパスを指定してコマンドプロンプトで実行
        psi.FileName = "cmd.exe"
        psi.Arguments = "/c """ & batPath & """"
        psi.UseShellExecute = False
        psi.RedirectStandardOutput = True
        psi.RedirectStandardError = True
        psi.CreateNoWindow = True

        Using p As New Process()
            p.StartInfo = psi
            p.Start()

            Dim stdout As String = p.StandardOutput.ReadToEnd()
            Dim stderr As String = p.StandardError.ReadToEnd()

            p.WaitForExit()

            Console.WriteLine("=== STDOUT ===")
            Console.WriteLine(stdout)

            If Not String.IsNullOrWhiteSpace(stderr) Then
                Console.WriteLine("=== STDERR ===")
                Console.WriteLine(stderr)
            End If

            Console.WriteLine("ExitCode: " & p.ExitCode)
        End Using

    End Sub

    'エラーログ
    Public Sub ErrLOG(ByVal C1 As String, ByVal C2 As String)
        Dim FileName As String
        Dim WS As IO.StreamWriter

        Try
            FileName = AppContext.BaseDirectory & "\Errlog.txt"
            WS = IO.File.AppendText(FileName)
            WS.WriteLine(C1 & ":" & C2 & ":" & Format(Now, "yyyy/MM/dd HH:mm:ss"))
            WS.Close()
        Catch ex As Exception
            End
        End Try
    End Sub

    Private Sub Init_xml()

        Try
            Dim iniFilePath As String = Path.Combine(AppContext.BaseDirectory, "INI.xml")
            If Not File.Exists(iniFilePath) Then
                Throw New FileNotFoundException("INI.xml が見つかりません。", iniFilePath)
            End If

            '指定したXMLファイルの読み込み
            Dim xmlDoc As New XmlDocument()
            xmlDoc.Load(iniFilePath)
            Dim rootElement As XmlElement = xmlDoc.DocumentElement

            If rootElement Is Nothing Then
                Throw New InvalidDataException("INI.xml のルート要素が見つかりません。")
            End If

            DBIP = GetRequiredTagValue(rootElement, "db_ip")
            DBPO = GetRequiredTagValue(rootElement, "db_port")
            DBNM = GetRequiredTagValue(rootElement, "db_name")
            DBID = GetRequiredTagValue(rootElement, "db_id")
            DBPW = GetRequiredTagValue(rootElement, "db_pw")
            'APPATH = GetRequiredTagValue(rootElement, "app_path")
            OutForder = GetRequiredTagValue(rootElement, "output_path")

        Catch ex As Exception
            Call ErrLOG("Init_xml", ex.Message)
        End Try

    End Sub
    Private Function GetRequiredTagValue(rootElement As XmlElement, tagName As String) As String
        Dim node = rootElement.SelectSingleNode(tagName)
        If node Is Nothing OrElse String.IsNullOrWhiteSpace(node.InnerText) Then
            Throw New InvalidDataException($"INI.xml のタグ '{tagName}' が未設定です。")
        End If

        Return node.InnerText.Trim()
    End Function

    'batファイル作成
    Private Sub Make_batFile()

        Dim FileName As String
        Dim WS As IO.StreamWriter
        Dim TD As String = Format(Now, "yyyyMMdd")
        '起動パス取得
        Dim MyPath As String = AppContext.BaseDirectory

        Try

            FileName = AppContext.BaseDirectory & "\export_view.bat"
            '既に存在する場合は削除
            If IO.File.Exists(FileName) Then
                IO.File.Delete(FileName)
            End If


            WS = IO.File.AppendText(FileName)

            WS.WriteLine("@echo off")
            WS.WriteLine("setlocal")
            WS.WriteLine("REM PostgreSQL接続情報")
            WS.WriteLine("Set ""PGHOST=" & DBIP & """")
            WS.WriteLine("Set ""PGPORT=" & DBPO & """")
            WS.WriteLine("Set ""PGDATABASE=" & DBNM & """")
            WS.WriteLine("Set ""PGUSER=" & DBID & """")
            WS.WriteLine("Set ""PGPASSWORD=" & DBPW & """")
            WS.WriteLine("REM 出力先")
            '日別ファイル
            WS.WriteLine("Set ""OUT_FILE=" & OutForder & "\" & TD & "_view_data.csv""")
            '既にファイルが存在する場合は削除
            WS.WriteLine("If exist ""%OUT_FILE%"" del ""%OUT_FILE%""")

            WS.WriteLine("REM ViewデータをCSV出力")
            Dim targetDate As String = Date.Today.AddDays(-1).ToString("yyMMdd")

            Dim sql As String =
                "\copy (SELECT * FROM public.view_shotdata " &
                "WHERE h03 = '" & targetDate & "') " &
                "TO '%OUT_FILE%' WITH (FORMAT csv, HEADER true, ENCODING 'SJIS')"

            WS.WriteLine($"""{MyPath}\psql.exe"" -v ON_ERROR_STOP=1 -c ""{sql}""")
            '起動フォルダ変更
            'WS.WriteLine($"""{APPATH}\psql.exe"" -v ON_ERROR_STOP=1 -c ""{sql}""")

            WS.WriteLine("If errorlevel 1 (")
            WS.WriteLine("  echo Export failed.")
            WS.WriteLine("  Exit /b 1")
            WS.WriteLine(")")
            WS.WriteLine("echo Export success: %OUT_FILE%")
            WS.WriteLine("Exit /b 0")
            WS.Close()

            '@echo off
            'setlocal
            'REM PostgreSQL接続情報
            'Set "PGHOST=localhost"
            'Set "PGPORT=5432"
            'Set "PGDATABASE=your_db"
            'Set "PGUSER=your_user"
            'Set "PGPASSWORD=your_password"
            'REM 出力先
            'Set "OUT_FILE=C:\temp\view_data.csv"
            'REM ViewデータをCSV出力
            'psql -v ON_ERROR_STOP=1 -c "\copy (SELECT * FROM public.your_view) TO '%OUT_FILE%' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')"
            'If errorlevel 1 (
            '  echo Export failed.
            '  Exit /b 1
            ')
            'echo Export success: %OUT_FILE%
            'Exit /b 0

            'EXCEL対応のため SJIS
            '"TO '%OUT_FILE%' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')"


        Catch ex As Exception
            Call ErrLOG("Make_batFile", ex.Message)
        End Try

    End Sub




End Module