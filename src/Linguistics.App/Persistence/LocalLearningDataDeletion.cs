using Linguistics.App.Diagnostics;
using Linguistics.App.Speech;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Persistence;

public static class LocalLearningDataDeletion
{
    public static async Task DeleteAllAsync(
        LearnerProfileOwner profileOwner,
        SpeechRecordingStore? speechRecordingStore,
        LocalDiagnosticLog? diagnosticLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profileOwner);

        if (speechRecordingStore is not null)
        {
            var deletion = await speechRecordingStore
                .DeleteAllAsync(cancellationToken)
                .ConfigureAwait(false);
            if (deletion.FailedFileCount > 0)
            {
                throw new LearnerStoreException(
                    "Some speech recordings owned by the app could not be deleted; learning data was kept so you can retry.");
            }
        }

        if (diagnosticLog is not null)
        {
            try
            {
                await diagnosticLog.DeleteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DiagnosticLogException exception)
            {
                throw new LearnerStoreException(
                    "The local diagnostic log could not be deleted; learning data was kept so you can retry.",
                    exception);
            }
        }

        await profileOwner.DeleteAllAsync(cancellationToken).ConfigureAwait(false);
    }
}
