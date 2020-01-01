namespace com.frannuca.network
open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Collections.Concurrent
open HtmlAgilityPack
open System.Web



module Craw=
    open Hierarchy

    type Crawler(maxdepth:int,quorumCard:int,stopWordsFile:string,logfile:string,dumpfile:string) =
        let logger = new MTLogger.ActorLogger(dumpfile,logfile)
        let tokenize(txt:string)=
            let tokens = Regex.Split(txt.ToLower(), "(?:(?<=^|[^a-zA-Z])'|'(?=[^a-zA-Z]|$)|[^a-zA-Z'])+")
            let stopwords = (File.ReadAllText(stopWordsFile)).Split(',')
                            |> Array.map(fun x -> x.Trim().ToLower()) |> Set.ofArray

            let words =          
                        tokens 
                        |> Seq.filter(fun word -> word.Length>=3 && not(stopwords.Contains(word)))

            let tokens = words |> Set.ofSeq

            let rc = new System.Collections.Generic.Dictionary<string,int>()
            tokens |> Set.iter(fun t -> rc.Add(t,0))
            words |> Seq.iter(fun word ->
                                        let w= rc.[word]
                                        rc.[word] <- w + 1
                                       )
            let a = rc |> Seq.sortByDescending(fun x -> x.Value)
            a

        // Extracts links from HTML.
        let extractLinks (tokens:string list)(exceptions:string list)(doc:HtmlDocument) =
            match doc.DocumentNode.SelectNodes("//a[@href]") with
            |null -> Seq.empty
            |s  ->s  |> Seq.map(fun link -> 

                                        let att = link.Attributes.["href"]
                                        let valx = att.Value
                                        let yy = valx.Split(' ') 
                                        yy |> Seq.filter(fun l -> l.StartsWith("http"))

                                    )
            |> Seq.collect(id)
            |> List.ofSeq
            |> List.map(fun s -> s.ToLower())
            |> List.distinct
            |> List.filter(fun l -> match tokens  with 
                                    |[] -> true
                                    | x -> x |> Seq.exists(fun t -> l.Contains(t.ToLower())))
            |> List.filter(fun l -> match exceptions  with 
                                       |[] -> true
                                       | x -> x |> Seq.forall(fun t -> not(l.Contains(t.ToLower()))))

        // Fetches a Web page.
        let fetch (url : string) = async{
                try
                    let req = WebRequest.Create(url) :?> HttpWebRequest
                    req.UserAgent <- "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)"
                    req.Timeout <- 5000
                    use! resp = req.GetResponseAsync() |> Async.AwaitTask
                    let content = resp.ContentType
                    let isHtml = Regex("html").IsMatch(content)
                    match isHtml with
                    | true -> use stream = resp.GetResponseStream()
                              use reader = new StreamReader(stream)
                              let! html = reader.ReadToEndAsync() |> Async.AwaitTask
                              let doc = new HtmlDocument()
                              doc.LoadHtml(html) 
                              let langattr = doc.DocumentNode.SelectSingleNode("//html").Attributes

                              if langattr.Contains("lang") && langattr.["lang"].Value.ToLower() = "en" then
                                  return Some(doc)
                              else
                                  return None
                              
                    | false -> return None
                with
                |ex ->  //printfn "%A" ex
                        return None
            }

                       
        let getNode(parent:string option)(depth:int,tokens:string list,exceptions:string list )(url:string)= async{
            //printfn "fetching %s with level %i" url depth
            let! doc = fetch url
            doc |> Option.map(fun _ ->  sprintf "%i%s%s" (depth)(String.replicate depth "\t" )(url) |> logger.Print2Log) |> ignore


            MTLogger.GetHTMLString([doc]) |> logger.Print2Dump

            if depth<=maxdepth then
                let links = doc |> Option.map(extractLinks(tokens)(exceptions)) |> Option.defaultValue []
                return {parent=parent;depth=depth;url = url;url_children=links|> Set.ofSeq;data=doc}
            else
                return {parent=parent;depth=depth;url = url;url_children=set([]);data=doc}
            }

        
        member self.Crawl(rooturl:string)=

            let queueurl = new Queue<Node>()
            let xnode = getNode None (0, [],[] ) rooturl  |> Async.RunSynchronously
            //extract main html string for tokenization:
            let treeroot = new TreeNode(xnode.url,xnode.data)
            let mainhtml = treeroot.Serialize2String()
            let tokens = tokenize mainhtml |> Seq.take(quorumCard) |> Seq.map(fun a -> a.Key) |> List.ofSeq
            String.Join(",",tokens |> Array.ofSeq) |> logger.Print2Log
            Console.ReadLine() |> ignore
            queueurl.Enqueue(xnode)
            let visited = new System.Collections.Concurrent.ConcurrentDictionary<string,Node>()
            let mutable counter = 0
            while(queueurl.Count > 0) do  
                let node = queueurl.Dequeue()
                match visited.ContainsKey(node.url) with
                |false ->
                            visited.[node.url] <- node
                            node.url_children 
                            |> Set.filter(fun url -> tokens |> Seq.exists(fun t -> url.ToLower().Contains(t)))
                            |> Array.ofSeq
                            |> Array.filter( visited.ContainsKey >> not)  
                            |> Array.map(getNode(Some(node.url))(node.depth+1,tokens,["youtube";"video";".pdf";".jpg";".png";".php"])) 
                            |> Async.Parallel 
                            |> Async.RunSynchronously 
                            |> Array.iter(queueurl.Enqueue)

                |true  -> ()

            let nodes= visited.Values 
            let root = 
                        let topnode = nodes |> Seq.filter(fun x -> x.depth=0) |> List.ofSeq |> List.head
                        new TreeNode(topnode.url,topnode.data)

            let rec buildTree(nodes:Node seq)(tree:TreeNode)=
                let children = nodes |> Seq.filter(fun x -> x.parent |> Option.defaultValue("") = tree.URL)
                                     |> Seq.map(fun x -> let xnode = new TreeNode(x.url,x.data)
                                                         xnode.Parent <- Some tree
                                                         xnode
                                                )
                                     |> Set.ofSeq

                tree.AddChildren children
                let thunk = buildTree(nodes |> Seq.filter(fun x -> x.url <> tree.URL))
                children |> Set.iter(thunk)
            printfn " BUILDING TREE ......."
            buildTree(nodes)(root)
            printfn " FINISHED TREE ......."
            root

