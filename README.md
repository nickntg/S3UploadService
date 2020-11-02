# S3 Upload Service
This is a simple Windows service that uploads files dropped in a Windows directory to S3.

To deploy as a service:
* Build the project.
* Publish to the enclosed profile (called WindowService).
* Register as a service using the sc command from an elevated command prompt.