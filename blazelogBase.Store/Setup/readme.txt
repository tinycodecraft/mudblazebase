Congratulations, 'EF Core Power Tools' has now generated a DbContext and Entity classes for you. 

You need to configure your app now - here are some hints:

### ASP.NET Core:

1. Register your DbContext class in your "Program.cs" file.

    ```csharp
    builder.Services.AddDbContext<BlazeLogDbContext>(
        options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    ```

2. Add "ConnectionStrings" to your configuration file (secrets.json, appsettings.Development.json or appsettings.json).

    ```json
    {
        "ConnectionStrings": {
            "DefaultConnection": "Data Source=10.23.231.12;Initial Catalog=EWBULDSHYD;User ID=sa;Password=sa;Trust Server Certificate=True"
        }
    }
    ```

### Thank you!

Thank you for using this free tool! Have a look at [the wiki](https://github.com/ErikEJ/EFCorePowerTools/wiki/Reverse-Engineering) 
to learn more about all the advanced features

You can create issues, questions and suggestions [on GitHub](https://github.com/ErikEJ/EFCorePowerTools/issues)

If you like my free tool, I would be very grateful for a rating or review 
on [Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=ErikEJ.EFCorePowerTools&ssr=false#review-details) 
or even a [one-time or monthly sponsorship](https://github.com/sponsors/ErikEJ?frequency=one-time&sponsor=ErikEJ)
