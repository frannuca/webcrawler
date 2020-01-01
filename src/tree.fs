namespace com.frannuca.network
open System
open System.Collections.Generic
open System.Text.RegularExpressions
open HtmlAgilityPack
open System.Web

module Hierarchy=
    type Node= {parent:string option;depth:int;url:string; url_children:string Set; data:HtmlDocument option}
        type TreeNode(url:string,data:HtmlDocument option) = 
            let mutable parent:TreeNode option = None
            let mutable children:TreeNode Set = Set([||])


            interface IComparable with
                member self.CompareTo(b)=
                    match b with
                    | :? TreeNode as x -> compare url (x.URL)
                    | _ -> failwith "Cannot compare TreeNode with unrelated types"

     
            member self.Parent 
                with get() = parent
                and set(value) = parent <- value

            member self.Depth=
                let rec depth(x:TreeNode)(c:int) =
                    match x.Parent with
                    |None -> c
                    |Some(p) -> depth p (c+1)
                depth self 0

            member self.Children = children
            member self.URL = url
            member self.AddChildren(childarr:TreeNode Set)=
                children <- Set.union children  childarr

            member self.BreadthFirst()=
                let q = new Queue<TreeNode>()
                q.Enqueue(self)
                let visited = new System.Collections.Generic.List<TreeNode>()
                while q.Count>0 do
                    let x = q.Dequeue()
                    visited.Add(x)
                    x.Children |> Seq.iter(q.Enqueue)
                visited

            member self.DepthFirst()=
                let s = new Stack<TreeNode>()
                let visited = new System.Collections.Generic.List<TreeNode>()
                s.Push self
                while s.Count>0 do
                    let x = s.Pop()
                    visited.Add(x)
                    x.Children |> Seq.iter(s.Push)
                visited

            member self.Serialize2String()=
                self.DepthFirst()
                |> Seq.map(fun x -> x.Data)
                |> MTLogger.GetHTMLString

            member self.Serialize2Disk(path:string)=
                System.IO.File.WriteAllText(path,self.Serialize2String())

            member self.Data=data

            override self.GetHashCode()=
                hash(url)

            override self.Equals(that)=
                match that with
                | :? TreeNode as x -> x.URL = url
                | _ -> false

            override self.ToString()=
                let arr=
                 self.DepthFirst()
                 |> Seq.map(fun n -> sprintf "%i%s%s" (n.Depth)(String.replicate n.Depth "\t" )(n.URL))
                String.Join("\n",arr)