using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-102 (the developer: "I don't want to be able to solve this level by jumping on a door while inverted"; "I don't
    // want the cat to reach a solution while being in the ceiling"): in L009 and L010 a flip turns a cat walking the roof
    // upside down back down before the door, so every route that finishes the level finishes standing. Red first: before
    // PAX-102 both solutions ended upside down at a door under the roof.
    public sealed class DoorUprightTests
    {
        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        [TestCase("L009")]
        [TestCase("L010")]
        public void EveryRouteThatFinishesTheLevel_FinishesStanding(string id)
        {
            object room = RouteValidatorTests.Room(id), routes = RouteValidatorTests.Routes(id);
            var finishing = new[] { F(routes, "Solution") }
                .Concat(((IEnumerable)F(routes, "Betrayals")).Cast<object>().Where(b => F(b, "Outcome").ToString() == "Recovers").Select(b => F(b, "Route")))
                .ToList();
            foreach (object route in finishing)
            {
                object replay = ReplayRoute(session, room, route);
                Assert.IsTrue((bool)F(replay, "Completed"), $"{id}: {F(route, "Name")} doesn't finish the level");
                var records = (IList)F(replay, "Records");
                Assert.IsFalse((bool)F(records[records.Count - 1], "GravityUp"), $"{id}: {F(route, "Name")} finishes upside down");
            }
        }
    }
}
