# BootstrapperUI
- Clone and Run the project
- In the URI text box enter the prefix for release environment you would like to point your bootstrapper to ex. for release-maul.integrate.team you would just put 'maul'
- Select the services on the left that you need connection strings generated for
- You can enter a file path to save the text to, or leave it blank and it will generate the text in the window
- You can check the "Set Env Variable" box to overwrite your CONSUL_SERVER environment variable. Any running instances of VS will need to be restarted in order for this change to take effect
- Copy the generated code, comment out the respective lines in the service and paste the new code in the RegisterServices function of the bootstrapper
