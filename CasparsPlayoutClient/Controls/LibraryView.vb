﻿'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
'' Author: Christopher Diekkamp
'' Email: christopher@development.diekkamp.de
'' GitHub: https://github.com/mcdikki
'' 
'' This software is licensed under the 
'' GNU General Public License Version 3 (GPLv3).
'' See http://www.gnu.org/licenses/gpl-3.0-standalone.html 
'' for a copy of the license.
''
'' You are free to copy, use and modify this software.
'' Please let me know of any changes and improvements you made to it.
''
'' Thank you!
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Imports CasparCGNETConnector
Imports logger

Public Class LibraryView

    Public WithEvents Library As Library
    Private Delegate Sub updateDelagete()
    Private cMenu As ContextMenuStrip

    Public Sub New()
        InitializeComponent()
        cmbRefresh.Image = Image.FromFile("img/refresh-icon.png")
        pbProgress.Image = Image.FromFile("img/refresh-icon-ani.gif")
        initMenu()

    End Sub

    Public Sub New(ByVal library As Library)
        Me.Library = library
        InitializeComponent()
        cmbRefresh.Image = Image.FromFile("img/refresh-icon.png")
        pbProgress.Image = Image.FromFile("img/refresh-icon-ani.gif")
        initMenu()
        refreshList()
        ' load last lib if pos.
        If My.Settings.rememberLibrary AndAlso My.Settings.last_Library.Length > 0 Then
            Dim xmlDoc As New MSXML2.DOMDocument()
            If xmlDoc.loadXML(My.Settings.last_Library) Then loadXml(xmlDoc)
        End If
    End Sub

    '
    '' Hilfsmethoden
    '

    Private Sub initMenu()
        '' Add ContexMenu
        cMenu = New ContextMenuStrip
        cMenu.Items.Add(New ToolStripMenuItem("Load from XML", Nothing, Sub() loadXml()))
        cMenu.Items.Add(New ToolStripMenuItem("Save library to xml", Nothing, Sub() saveXmlLib()))
        Me.ContextMenuStrip = cMenu
    End Sub

    Public Sub saveXmlLib()
        If Library.getItems.Count > 0 Then
            Dim domDoc As New MSXML2.DOMDocument
            Dim pnode = domDoc.createElement("library")

            For Each m In Library.getItems
                pnode.appendChild(m.toXml.firstChild)
            Next

            domDoc.appendChild(pnode)
            domDoc.save("MediaLibrary.xml")

            logger.log("LibraryView.saveXmlLib: Media library successfully saved.")
        Else
            logger.warn("LibraryView.saveXmlLib: Library is empty. Nothing to save.")
        End If
    End Sub

    Public Function getXmlLib() As String
        Dim domDoc As New MSXML2.DOMDocument
        Dim pnode = domDoc.createElement("library")

        For Each m In Library.getItems
            pnode.appendChild(m.toXml.firstChild)
        Next

        domDoc.appendChild(pnode)
        Return domDoc.xml
    End Function

    Public Sub loadXml()
        Dim fd As New OpenFileDialog()
        fd.DefaultExt = "xml"
        fd.Filter = "Xml Dateien|*.xml"
        fd.CheckFileExists = True
        fd.Multiselect = True
        fd.ShowDialog()

        For Each f In fd.FileNames
            loadXml(f)
        Next
        applyFilter()
    End Sub

    Public Sub loadXml(ByVal fileName As String)
        Dim domDoc As New MSXML2.DOMDocument
        Dim media As AbstractCasparCGMedia
        If domDoc.load(fileName) Then
            If domDoc.firstChild.nodeName.Equals("library") Then
                ' load whole lib
                For Each m As MSXML2.IXMLDOMNode In domDoc.firstChild.selectNodes("media")
                    media = CasparCGMediaFactory.createMedia(m.xml)
                    If Not IsNothing(media) Then
                        Library.addItem(media)
                        addMediaItem(media)
                        logger.log("LibraryView.loadXml: Successfully loaded " & media.getName & " from '" & fileName & "'.")
                    End If
                Next
            ElseIf domDoc.firstChild.nodeName.Equals("media") Then
                ' single media
                media = CasparCGMediaFactory.createMedia(domDoc.xml)
                If Not IsNothing(media) Then
                    Library.addItem(media)
                    addMediaItem(media)
                    logger.log("LibraryView.loadXml: Successfully loaded " & media.getName & " from '" & fileName & "'.")
                End If
            Else
                logger.warn("LibraryView.loadXml: Unable to load media from '" & fileName & "'. Not a valid media definition.")
            End If
        Else
            logger.err("LibraryView.loadXml: Unable to parse media file '" & fileName & "'. Not a valid xml file.")
        End If
        applyFilter()
    End Sub

    Public Sub loadXml(ByRef xmlDoc As MSXML2.DOMDocument)
        Dim media As AbstractCasparCGMedia
        If xmlDoc.hasChildNodes Then
            If xmlDoc.firstChild.nodeName.Equals("library") Then
                ' load whole lib
                For Each m As MSXML2.IXMLDOMNode In xmlDoc.firstChild.selectNodes("media")
                    media = CasparCGMediaFactory.createMedia(m.xml)
                    If Not IsNothing(media) Then
                        Library.addItem(media)
                        addMediaItem(media)
                        logger.log("LibraryView.loadXml: Successfully loaded " & media.getName)
                    End If
                Next
            ElseIf xmlDoc.firstChild.nodeName.Equals("media") Then
                ' single media
                media = CasparCGMediaFactory.createMedia(xmlDoc.xml)
                If Not IsNothing(media) Then
                    Library.addItem(media)
                    addMediaItem(media)
                    logger.log("LibraryView.loadXml: Successfully loaded " & media.getName)
                End If
            Else
                logger.warn("LibraryView.loadXml: Unable to load media. Not a valid media definition.")
            End If
        Else
            logger.err("LibraryView.loadXml: Unable to load media. Empty definition.")
        End If
        applyFilter()
    End Sub


    Private Sub applyFilter() Handles ckbAudio.CheckedChanged, ckbMovie.CheckedChanged, ckbStill.CheckedChanged, ckbTemplate.CheckedChanged, txtFilter.TextChanged
        Dim filteredList As New List(Of AbstractCasparCGMedia)

        ' Filter by type
        If ckbAudio.Checked Then
            filteredList.AddRange(Library.getItemsOfType(AbstractCasparCGMedia.MediaType.AUDIO))
        End If
        If ckbMovie.Checked Then
            filteredList.AddRange(Library.getItemsOfType(AbstractCasparCGMedia.MediaType.MOVIE))
        End If
        If ckbStill.Checked Then
            filteredList.AddRange(Library.getItemsOfType(AbstractCasparCGMedia.MediaType.STILL))
        End If
        If ckbTemplate.Checked Then
            filteredList.AddRange(Library.getItemsOfType(AbstractCasparCGMedia.MediaType.TEMPLATE))
        End If

        ' Filter by name
        If txtFilter.Text.Length > 0 Then
            For Each item In filteredList

                '' ist das item im Filterergebnis?
                If item.getFullName.ToUpper Like txtFilter.Text.ToUpper & "*" OrElse item.getName.ToUpper Like txtFilter.Text.ToUpper & "*" Then
                    ' Gibt es schon ein entsprechendes control?
                    If layoutItemsFlow.Controls.ContainsKey(item.getFullName) Then
                        ' Also sichtbar machen
                        layoutItemsFlow.Controls.Item(layoutItemsFlow.Controls.IndexOfKey(item.getFullName)).Visible = True
                    Else
                        ' Noch kein Control da, also hinzufügen (Sollte eigentlich nicht vorkommen)
                        addMediaItem(item)
                    End If
                Else
                    ' Das item ist nicht im ergebnis, wenn es ein entsprechendes Control gibt wird es unsichtbar gemacht.
                    If layoutItemsFlow.Controls.ContainsKey(item.getFullName) Then
                        layoutItemsFlow.Controls.Item(layoutItemsFlow.Controls.IndexOfKey(item.getFullName)).Visible = False
                    End If
                End If
            Next
        Else
            For Each item As LibraryViewItem In layoutItemsFlow.Controls
                If filteredList.Contains(item.MediaItem) Then
                    ' Also sichtbar machen
                    item.Visible = True
                Else
                    ' Noch kein Control da, also hinzufügen (Sollte eigentlich nicht vorkommen)
                    item.Visible = False
                End If
            Next
        End If
    End Sub

    Private Sub refreshList() Handles Library.updated
        If InvokeRequired Then
            Invoke(New updateDelagete(AddressOf refreshList))
        Else
            layoutItemsFlow.Controls.Clear()
            If Not IsNothing(Library) Then
                For Each item In Library.getItems
                    addMediaItem(item)
                Next
            End If
            If My.Settings.rememberLibrary AndAlso Library.getItems.Count > 0 Then My.Settings.last_Library = getXmlLib()
            applyFilter()
        End If
    End Sub

    ''' <summary>
    ''' Adds a CasparCGMedia to the LibraryView
    ''' </summary>
    ''' <param name="mediaItem"></param>
    ''' <remarks></remarks>
    Private Sub addMediaItem(ByRef mediaItem As AbstractCasparCGMedia)
        Dim libItem As New LibraryViewItem(mediaItem)
        layoutItemsFlow.Controls.Add(libItem)
        libItem.Width = libItem.Parent.ClientRectangle.Width - libItem.Parent.Margin.Horizontal
    End Sub

    '
    '' Ereignis verarbeitung
    '
    Private Sub cmbRefresh_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles cmbRefresh.Click
        If Not IsNothing(Library) Then
            cmbRefresh.Visible = False
            pbProgress.Visible = True
            Library.refreshLibrary()
        End If
    End Sub

    Private Sub LibraryView_ClientSizeChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.ClientSizeChanged, layoutItemsFlow.ClientSizeChanged
        For Each item As Control In layoutItemsFlow.Controls
            item.Width = item.Parent.ClientRectangle.Width - item.Parent.Margin.Horizontal
        Next
    End Sub

    Private Sub onUpdate() Handles Library.updatedAborted, Library.updated
        If InvokeRequired Then
            Invoke(New updateDelagete(AddressOf onUpdate))
        Else
            pbProgress.Visible = False
            cmbRefresh.Visible = True
        End If
    End Sub

End Class
