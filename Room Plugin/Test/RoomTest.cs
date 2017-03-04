using NUnit.Framework;

namespace Room_Plugin.Test {

    [TestFixture]
    class RoomTest {

        private Room room;

        [SetUp]
        public void Init () {
            
            room = new Room();
        }

        [Test]
        public void ShouldGetSetMaxPlayers () {

            const int maxPlayersValue = 2;
            room.SetMaxPlayers(maxPlayersValue);

            Assert.That(room.GetMaxPlayers(), Is.EqualTo(maxPlayersValue));
        }

        [Test]
        public void ShouldGetSetName () {

            const string nameValue = "My name!";
            room.SetName(nameValue);

            Assert.That(room.GetName(), Is.EqualTo(nameValue));
        }

        [Test]
        /**
         * Tests adding a player to a room by checking that the player's ID was added to the list
         * and that the length is correct.
         */
        public void ShouldAddPlayer () {

            room.SetMaxPlayers(8);

            const ushort playerId1 = 1;
            room.AddPlayer(playerId1);

            Assert.That(room.Players.Count, Is.EqualTo(1));
            Assert.That(room.Players, Has.Exactly(1).EqualTo(playerId1));

            const ushort playerId2 = 2;
            room.AddPlayer(playerId2);

            Assert.That(room.Players.Count, Is.EqualTo(2));
            Assert.That(room.Players, Has.Exactly(1).EqualTo(playerId2));
        }

        [Test]
        public void ShouldRemovePlayer () {

            room.SetMaxPlayers(8);

            // Test on empty list
            room.RemovePlayer(10000);
            Assert.That(room.Players.Count, Is.EqualTo(0));

            // Add a player and remove them
            const ushort playerId1 = 1;
            room.AddPlayer(playerId1);
            room.RemovePlayer(playerId1);

            Assert.That(room.Players.Count, Is.EqualTo(0));
            Assert.That(room.Players, Has.Exactly(0).EqualTo(playerId1));

            // Add a player and remove someone else that doesn't exist
            const ushort playerId2 = 2;
            room.AddPlayer(playerId2);
            room.RemovePlayer(playerId1);
            Assert.That(room.Players.Count, Is.EqualTo(1));
            Assert.That(room.Players, Has.Exactly(1).EqualTo(playerId2));
        }

        [Test]
        public void ShouldVerifyPlayerExistence () {
            
            room.SetMaxPlayers(8);
            const ushort playerId1 = 1;
            const ushort playerId2 = 2;

            // Check if anyone exists... the world is a lonely place sometimes.
            Assert.That(room.PlayerExists(playerId1), Is.EqualTo(false));

            // Add a player and check if they exist
            room.AddPlayer(playerId1);
            Assert.That(room.PlayerExists(playerId1), Is.EqualTo(true));
            
            // Ensure player 2 doesn't exist
            Assert.That(room.PlayerExists(playerId2), Is.EqualTo(false));

            // Remove player 1 and ensure they no longer exist
            room.RemovePlayer(playerId1);
            Assert.That(room.PlayerExists(playerId1), Is.EqualTo(false));
        }
    }
}
