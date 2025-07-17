using iCUE_ReverseEngineer.Client;

namespace iCUE_ReverseEngineer;

public class KeyIdTest
{
    public async Task Run()
    {
        var gameClient = new GsiClient(ClientType.Sdk);
        await gameClient.Run();

        Console.WriteLine($"Press enter for next keyId");
        var keyId = 1;
        while (true)
        {
            Console.ReadLine();

            await gameClient.SetColor(keyId - 1, 0, 0, 0); // Set to red color
            await gameClient.SetColor(keyId, 255, 0, 0); // Set to red color
            Console.WriteLine($"Set color for LED {keyId} to red.");
            keyId++;
        }
    }
}