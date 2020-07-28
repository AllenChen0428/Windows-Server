Imports System.ComponentModel
Imports System.Configuration.Install

Public Class ProjectInstaller

    Public Sub New()
        MyBase.New()

        '此為元件設計工具所需的呼叫。
        InitializeComponent()

        '在呼叫 InitializeComponent 之後加入初始化程式碼

    End Sub

    Private Sub ServiceProcessInstaller1_AfterInstall(sender As Object, e As InstallEventArgs) Handles ServiceProcessInstaller1.AfterInstall

    End Sub

    Private Sub ServiceInstaller1_AfterInstall(sender As Object, e As InstallEventArgs) Handles ServiceInstaller1.AfterInstall

    End Sub

    Private Sub ServiceInstaller2_AfterInstall(sender As Object, e As InstallEventArgs) 

    End Sub
End Class
