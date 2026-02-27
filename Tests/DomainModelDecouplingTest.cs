using Godot;
using NUnit.Framework;

namespace Archistrateia.Tests
{
    [TestFixture]
    public class DomainModelDecouplingTest
    {
        [Test]
        public void DomainModels_Should_Not_Inherit_From_Godot_Node()
        {
            Assert.IsFalse(typeof(Node).IsAssignableFrom(typeof(City)), "City should be a plain domain object.");
            Assert.IsFalse(typeof(Node).IsAssignableFrom(typeof(Player)), "Player should be a plain domain object.");
            Assert.IsFalse(typeof(Node).IsAssignableFrom(typeof(Unit)), "Unit should be a plain domain object.");
        }

        [Test]
        public void City_SetOwner_Should_Update_Player_City_Collections()
        {
            var playerOne = new Player("P1", 100);
            var playerTwo = new Player("P2", 100);
            var city = new City("Memphis");

            city.SetOwner(playerOne);
            city.SetOwner(playerTwo);

            Assert.AreSame(playerTwo, city.Owner);
            Assert.IsFalse(playerOne.Cities.Contains(city), "Previous owner should lose the city.");
            Assert.IsTrue(playerTwo.Cities.Contains(city), "New owner should gain the city.");
        }

        [Test]
        public void Player_ResetUnitMovement_Should_Reset_All_Controlled_Units()
        {
            var player = new Player("P1", 100);
            var nakhtu = new Nakhtu { CurrentMovementPoints = 0, HasMoved = true };
            var medjay = new Medjay { CurrentMovementPoints = 1, HasMoved = true };
            player.AddUnit(nakhtu);
            player.AddUnit(medjay);

            player.ResetUnitMovement();

            Assert.AreEqual(nakhtu.MovementPoints, nakhtu.CurrentMovementPoints);
            Assert.AreEqual(medjay.MovementPoints, medjay.CurrentMovementPoints);
            Assert.IsFalse(nakhtu.HasMoved);
            Assert.IsFalse(medjay.HasMoved);
        }
    }
}
