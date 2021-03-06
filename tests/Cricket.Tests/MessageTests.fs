﻿namespace Cricket.Tests

open System
open System.Threading
open NUnit.Framework
open FsUnit
open Cricket

type RecordingActor(path) =
    let messages = new ResizeArray<Message<int>>()
    
    interface IActor with
        member x.Path with get() = ActorPath.ofString path
        member x.Post(msg) = messages.Add(Message.map unbox msg)
        member x.Dispose() = ()

[<TestFixture; Category("Unit"); Ignore("Temp failing on build server")>]
type ``Given an Message Handler``() =
    
    let getCell() =
        let simpleMailbox = new DefaultMailbox<Message<int>>("test")
        ActorCell<int>.Create(new RecordingActor("actor/test"), simpleMailbox)

    [<Test>]
    member __.``I can recieve a message``() =
        let cell = getCell()
        let resultGate = new ManualResetEvent(false)
        let result = ref 0

        messageHandler {
            let! msg = Message.receive()
            result := msg
            resultGate.Set() |> ignore
        } |> MessageHandler.toAsync cell |> Async.Start

        cell.Mailbox.Post(Message.create<int> None 10)

        if resultGate.WaitOne(1000)
        then !result |> should equal 10
        else Assert.Fail("No result timeout")

    [<Test>]
    member __.``I can scan for a message``() =
        let cell = getCell()
        let resultGate = new ManualResetEvent(false)
        let result = ref 0

        messageHandler {
            let! msg = Message.scan(fun msg -> if msg.Message = 10 then Some (async { return msg }) else None)
            result := msg
            resultGate.Set() |> ignore
        } |> MessageHandler.toAsync cell |> Async.Start

        let producer = 
            async {
                do cell.Mailbox.Post(Message.create None 2)
                do! Async.Sleep(400)
                do cell.Mailbox.Post(Message.create None 6)
                do! Async.Sleep(400)
                do cell.Mailbox.Post(Message.create None 10)
            }

        Async.Start(producer)

        if resultGate.WaitOne(1000)
        then !result |> should equal 10
        else Assert.Fail("No result timeout") 

    [<Test>]
    member __.``I can time out scan for a message``() =
        let cell = getCell()
        let resultGate = new ManualResetEvent(false)
        let result = ref (Some 10)

        messageHandler {
            let! msg = Message.tryScan 100 (fun msg -> if msg.Message = 10 then Some (async { return msg }) else None)
            result := msg
            resultGate.Set() |> ignore
        } |> MessageHandler.toAsync cell |> Async.Start

        let producer = 
            async {
                do! Async.Sleep(400)
                do cell.Mailbox.Post(Message.create None 2)
                do! Async.Sleep(400)
                do cell.Mailbox.Post(Message.create None 6)
                do! Async.Sleep(400)
                do cell.Mailbox.Post(Message.create None 10)
            }

        Async.Start(producer)

        if resultGate.WaitOne(1000)
        then !result |> should equal None
        else Assert.Fail("No result timeout") 

    [<Test>]
    member __.``I can timeout recieving a message``() =
        let cell = getCell()
        let resultGate = new ManualResetEvent(false)
        let result = ref (Some 10)

        messageHandler {
            let! msg = Message.tryReceive 100
            result := msg
            resultGate.Set() |> ignore
        } |> MessageHandler.toAsync cell |> Async.Start

      
        if resultGate.WaitOne(1000)
        then !result |> should equal None
        else Assert.Fail("No result timeout")

    [<Test>]
    member __.``I can recieve a message and get the sender``() =
        let cell = getCell()
        let resultGate = new ManualResetEvent(false)
        let result = ref ActorSelection.empty

        messageHandler {
            let! msg = Message.receive()
            let! sender = Message.sender()
            result := sender
            resultGate.Set() |> ignore
        } |> MessageHandler.toAsync cell |> Async.Start

        cell.Mailbox.Post(Message.create<int> (Some cell.Self) 10)

        if resultGate.WaitOne(1000)
        then !result |> should equal (ActorSelection([cell.Self]))
        else Assert.Fail("No result timeout")



