namespace com.frannuca.network
open System
open System.Text.RegularExpressions
open HtmlAgilityPack
open System.Web

module MTLogger=
    let private cleanHtml (n:HtmlNode) =
        let html = HttpUtility.HtmlDecode(n.InnerHtml)
        if not(String.IsNullOrEmpty(html)) then
            Regex.Replace(html,"<.*?>|\[d+\]",String.Empty)

        else
            String.Empty
    let GetHTMLString (doc:HtmlDocument option seq)=
        doc
        |> Seq.map(fun node -> 
                            node
                            |> Option.map(fun (x:HtmlDocument) -> 
                                            try
                                                x.DocumentNode.SelectNodes("//title[normalize-space(text())]|//p[normalize-space(text())]|//h1[normalize-space(text())]") 
                                                |> Array.ofSeq
                                            with
                                            |  _ -> 
                                                    [||]
                                            )
                            |> Option.defaultValue([||])
                    )
        |> Seq.collect(id)
        |> Seq.map(fun n -> cleanHtml(n)+ "\n")
        |> Seq.fold(fun a b -> a + b) ""

    type LogMessage=
        |DumpMsg of string
        |LogMsg of string
        |Stop 

    type ActorLogger(path2dump:string,logfile:string)=
        let file2dump = new System.IO.StreamWriter(path2dump)
        let file2log  = new System.IO.StreamWriter(logfile)

        let  _mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop() = async { 
                            let! msg = inbox.Receive()
                            match msg with
                            |DumpMsg(msg) -> file2dump.WriteLine(msg)
                                             return! loop()
                            |LogMsg(p) ->
                                              printf "%s" p
                                              file2log.WriteLine(p)
                                              return! loop()
                                              
                            |Stop -> printfn "Leaving mailbox"
                                     return ()

                           }
                loop()
               )
        interface System.IDisposable with
            override self.Dispose()=
                Stop |> _mailbox.Post
                file2dump.Close()
                file2log.Close()

        member self.Print2Dump(msg:string)=
             DumpMsg(msg) |> _mailbox.Post

        member self.Print2Log(p:string)=
             LogMsg(p) |> _mailbox.Post